using MechaSys.SoftBricks.Jobs;
using MechaSys.SoftBricks.LoadPorts;
using MechaSys.SoftBricks.Materials;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QMC.Jobs
{
    public class GeneralSubstrateInCarrierTransferJobCalculator : SubstrateInCarrierTransferJobCalculator
    {
        #region Constructor
        /// <summary>
        /// GeneralSubstrateInCarrierTransferJobCalculator 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="owner">개체를 소유한 작업입니다.</param>
        public GeneralSubstrateInCarrierTransferJobCalculator(TransferJob owner)
            : base(owner)
        {
        }
        /// <summary>
        /// GeneralSubstrateInCarrierTransferJobCalculator 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        public GeneralSubstrateInCarrierTransferJobCalculator() : this(null) { }
        #endregion

        #region Method
        private bool CheckJobOrderToReceive(StackCarrier carrier, out Material material)
        {
            int slotIndex = -1;
            bool foundedJobOrder = false;
            Substrate substrate = null;
            ProcessJob processJob = null;
            JobOrder jobOrder = null;
            JobPriorityCollection priorities = new JobPriorityCollection();

            material = null;

            priorities.Clear();

            for(int i = 0; i < carrier.Capacity; i++)
            {
                foundedJobOrder = false;

                #region check material presence
                material = carrier.Ports[i].Location.GetMaterial();
                if(this.CheckMaterialPresenceInCarrier(carrier, i) == false)
                {
                    priorities.Add(JobPriority.DisablePriority);
                    continue;
                }
                #endregion

                #region check process job
                substrate = material as Substrate;
                if(substrate == null)
                {
                    priorities.Add(JobPriority.DisablePriority);
                    continue;
                }

                // Revision 2
                if(string.IsNullOrEmpty(substrate.ProcessJobId) == false)
                {
                    // process job
                    processJob = ProcessJobManager.GetByJobId(substrate.ProcessJobId);
                    if(processJob == null)
                    {
                        priorities.Add(JobPriority.DisablePriority);
                        continue;
                    }

                    // process job state
                    if(processJob.State.CurrentStateValue != ProcessJobStateMachine.StateEnum.Processing)
                    {
                        // Stopping 중이면서 SubstrateTransportState.AtWork인 것은 꺼내야 된다
                        // (added by biglake 2015/07/09. Carrier를 이용하여 Batch작업을 하는 경우를 대비)
                        if(processJob.State.CurrentStateValue != ProcessJobStateMachine.StateEnum.Stopping ||
                            substrate.TransportState.CurrentStateValue != SubstrateTransportStateMachine.StateEnum.AtWork)
                        {
                            priorities.Add(JobPriority.DisablePriority);
                            continue;
                        }
                    }
                }
                #endregion

                #region check current job order
                // To Do: process job의 우선 순위를 반영하여야 한다.
                jobOrder = material.JobOrders.GetCurrentJob();
                if(jobOrder == null || jobOrder.State != JobOrderState.Ready)
                {
                    priorities.Add(JobPriority.DisablePriority);
                    continue;
                }

                foreach(JobOrder innerJobOrder in jobOrder.JobOrders)
                {
                    TransferJobOrder transferJobOrder = innerJobOrder as TransferJobOrder;
                    if(transferJobOrder == null) continue;
                    if(transferJobOrder.Primary == this.Owner.PrimaryModule.Name &&
                        transferJobOrder.PrimaryPort == this.Owner.PrimaryPort &&
                        transferJobOrder.Secondary == this.Owner.SecondaryModule.Name &&
                        transferJobOrder.Direction == this.Owner.TransferDirection)
                    {
                        foundedJobOrder = true;
                        break;
                    }
                }
                #endregion

                if(foundedJobOrder == false)
                {
                    priorities.Add(JobPriority.DisablePriority);
                    continue;
                }
                else
                {
                    JobPriority priority = this.DefaultPriority;
                    priority.Material = material.Priority;
                    priorities.Add(priority);
                }

                //// add secondary port list.
                //this.Owner.SecondaryPorts.Add(i);
                //// primary port count와 secondary port count가 동일할때 까지.
                //// To Do: port count보다는 location count를 확인하여야 한다.
                //if (this.Owner.PrimaryPorts.Count == this.Owner.SecondaryPorts.Count) break;
            } // end of for

            #region get slot index
            slotIndex = priorities.GetMaxIndex();
            if(slotIndex < 0)
            {
                this.DisableReason = string.Format("The receive job order was not found");
                return false;
            }
            else
            {
                this.Owner.SecondaryCarrierPorts.Add(slotIndex);
                material = carrier.Ports[slotIndex].Location.GetMaterial();
            }
            #endregion

            return true;
        }
        private bool CheckJobOrderToSend(StackCarrier carrier, out Material material)
        {
            bool foundedJobOrder = false;
            JobOrder jobOrder = null;
            JobPriorityCollection priorities = new JobPriorityCollection();

            material = null;

            for(int i = 0; i < this.Owner.PrimaryPorts.Count; i++)
            {
                foundedJobOrder = false;

                #region check material presence
                material = this.Owner.Primary.Ports[this.Owner.PrimaryPorts[i]].Location.GetMaterial();
                if(material.Presence != MaterialPresence.Exist)
                {
                    this.DisableReason = "Material does not exists";
                    return false;
                }
                #endregion

                #region check material size
                Wafer wafer = material as Wafer;
                StackCarrier waferCarrier = carrier as StackCarrier;
                if(wafer != null && waferCarrier != null)
                {
                    if(wafer.Size != waferCarrier.Size)
                    {
                        this.DisableReason = string.Format("A size of the wafer[{0}] is not equal a size of carrier[{1}]", wafer.Size, waferCarrier.Size);
                        return false;
                    }
                }
                #endregion

                #region check job order
                jobOrder = material.JobOrders.GetCurrentJob();
                if(jobOrder != null && jobOrder.State == JobOrderState.Ready)
                {
                    foreach(JobOrder innerJobOrder in jobOrder.JobOrders)
                    {
                        TransferJobOrder transferJobOrder = innerJobOrder as TransferJobOrder;
                        if(transferJobOrder == null) continue;
                        if(transferJobOrder.Primary == this.Owner.PrimaryModule.Name &&
                            transferJobOrder.PrimaryPort == this.Owner.PrimaryPort &&
                            transferJobOrder.Secondary == this.Owner.SecondaryModule.Name &&
                            transferJobOrder.SecondaryPort == this.Owner.SecondaryPort &&
                            transferJobOrder.Direction == this.Owner.TransferDirection)
                        {
                            for(int j = 0; j < transferJobOrder.SecondaryCarrierPorts.Count; j++)
                            {
                                if(this.CheckMaterialPresenceInCarrier(carrier, transferJobOrder.SecondaryCarrierPorts[j]) == true)
                                {
                                    this.Owner.SecondaryCarrierPorts.Add(transferJobOrder.SecondaryCarrierPorts[j]);
                                    foundedJobOrder = true;
                                    break;
                                }
                            }
                            if(foundedJobOrder == true) break;
                        }
                    }
                }

                if(foundedJobOrder == false)
                {
                    this.DisableReason = "The send job order was not found";
                    return false;
                }
                #endregion
            }

            return true;
        }
        #endregion

        #region SubstrateInCarrierTransferJobCalculator Members
        protected override bool CheckJobOrderToReceive(Carrier carrier, out Material material)
        {
            if(carrier is StackCarrier stackCarrier && stackCarrier.Above != null)
            {
                if(base.CheckJobOrderToReceive(carrier, out material) == true) return true;
                else if(this.CheckJobOrderToReceive(stackCarrier.Above, out material) == true) return true;
                else
                    return false;
            }
            return base.CheckJobOrderToSend(carrier, out material);
        }

        protected override bool CheckJobOrderToSend(Carrier carrier, out Material material)
        {
            if(carrier is StackCarrier stackCarrier && stackCarrier.Above != null)
            {
                if(base.CheckJobOrderToSend(carrier, out material) == true) return true;
                else if(this.CheckJobOrderToSend(stackCarrier.Above, out material) == true) return true;
                else
                    return false;
            }
            return base.CheckJobOrderToSend(carrier, out material);
        }
        #endregion
    }
}
