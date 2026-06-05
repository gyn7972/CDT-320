using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.Jobs;
using MechaSys.SoftBricks.LoadPorts;
using MechaSys.SoftBricks.LoadPorts.SlotMappers;
using MechaSys.SoftBricks.Processes;
using MechaSys.SoftBricks.Secs.Jobs;
using MechaSys.SoftBricks.Transfer;

using QMC.Equipments;
using QMC.Transfers.Feeders;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace QMC.Jobs
{
    #region GeneralJobDispatcherCreator
    public class GeneralJobDispatcherCreator : JobDispatcherCreator
    {
        #region Field
        protected const int DefaultPickJobPriority = 20000;
        public const int DefaultLoadPortPickJobPriority = 30000;
        protected const int DefaultTransferablePickJobPriority = 80000;

        protected const int DefaultPickReadyToTransferJobPriority = 50;

        protected const int DefaultPlaceJobPriority = 90000;
        protected const int DefaultLoadPortPlaceJobPriority = 70000;

        protected const int DefaultPickAndPlaceJobPriority = 85000; // DefaultPlaceJobPriority 보다는 적게 pick 보다는 크게

        protected const int DefaultMappingJobPriority = 10000;
        protected const int DefaultSelfMappingJobPriority = 10000;

        protected const int DefaultRunRecipeJobPriority = 10000;
        protected const int DefaultPreAlignJobPriority = 10000;
        #endregion

        #region Constructor
        public GeneralJobDispatcherCreator(Nameable nameable)
            : base(nameable)
        {
        }
        public GeneralJobDispatcherCreator() : this(new Nameable()) { }
        #endregion

        #region Property
        #endregion

        #region Method
        protected virtual int GetTransferJobDispatcher(Feeder feeder, ref JobDispatcher dispatcher)
        {
            int ret = 0;
            ITransferred transferred;
            TransferItem loadPort = null;
            JobCollection jobs;

            #region Feeder가 접근 가능한 LoadPort 를 찾는다
            for (int i = 0; i < feeder.TransferItems.Count; i++)
            {
                transferred = Sys.Equipment.Modules.GetByTypeAndName<ITransferred>(feeder.TransferItems[i].TransferredModule);
                if (transferred == null)
                    throw new ArgumentException(string.Format("The transferred module [{0}] is not found.", feeder.TransferItems[i].TransferredModule));

                if (transferred is LoadPort)
                {
                    loadPort = feeder.TransferItems[i];
                    break;
                }
            }
            // Feeder 가 접근 가능한 LoadPort 가 없는 경우
            if (loadPort == null)
            {
                for (int i = 0; i < feeder.TransferItems.Count; i++)
                {
                    if ((ret = this.GetTransferJobs(feeder, feeder.TransferItems[i], out jobs)) != 0) return ret;
                    dispatcher.Jobs.AddRange(jobs);
                }
                //To Do :
            }
            else
            {
                #region Add job dispatcher
                for (int i = 0; i < feeder.TransferItems.Count; i++)
                {
                    transferred = Sys.Equipment.Modules.GetByTypeAndName<ITransferred>(feeder.TransferItems[i].TransferredModule);
                    if (transferred == null)
                        throw new ArgumentException(string.Format("The transferred module [{0}] is not found.", feeder.TransferItems[i].TransferredModule));
                    if (transferred is LoadPort) continue;
                    if ((ret = this.GetTransferJobs(feeder, loadPort, feeder.TransferItems[i], out jobs)) != 0) return ret;
                    dispatcher.Jobs.AddRange(jobs);
                }
                #endregion
            }
            #endregion

            return ret;
        }
        protected virtual int GetTransferJobs(Feeder feeder, TransferItem loadPort, TransferItem transferItem, out JobCollection jobs)
        {
            int ret = 0;
            Int32Collection transferredPorts, loadPortTransferredPorts;
            JobCollection pickJobs, placeJobs;

            ITransferred transferred = Sys.Equipment.Modules.GetByName(transferItem.TransferredModule) as ITransferred;
            LoadPort loadport = Sys.Equipment.Modules.GetByName(loadPort.TransferredModule) as LoadPort;

            #region exception
            if (feeder == null)
                throw new ArgumentNullException("Transferable");
            if (loadport == null)
                throw new ArgumentNullException("LoadPort");
            if (transferred == null)
                throw new ArgumentNullException("Transferred");
            #endregion

            jobs = new JobCollection();

            #region get Ports
            //설정되지 않은 경우는 전체 port
            //if(0 < loadPort.TransferablePorts.Count)
            //    loadPortTransferredPorts = loadPort.TransferablePorts;
            //else
            loadPortTransferredPorts = loadport.Ports.GetPortIndexes();

            // 설정되지 않은 경우는 전체 port
            if (0 < transferItem.TransferredPorts.Count)
                transferredPorts = transferItem.TransferredPorts;
            else
                transferredPorts = transferred.Ports.GetPortIndexes();
            #endregion

            //pick
            if ((ret = this.GetPickJobs(feeder, loadport, loadPortTransferredPorts, transferred, transferredPorts, out pickJobs)) != 0) return ret;
            jobs.Add(pickJobs);

            if ((ret = this.GetPlaceJobs(feeder, loadport, loadPortTransferredPorts, transferred, transferredPorts, out placeJobs)) != 0) return ret;
            jobs.Add(placeJobs);

            return ret;
        }
        protected virtual int GetPickJobs(Feeder feeder, LoadPort loadPort, Int32Collection loadPortTransferredPorts, ITransferred transferred, Int32Collection transferredPorts, out JobCollection jobs)
        {
            int ret = 0;
            Job pick = null;
            jobs = new JobCollection();

            for (int i = 0; i < loadPortTransferredPorts.Count; i++)
            {
                for (int j = 0; j < transferredPorts.Count; j++)
                {
                    if ((ret = this.GetPickJob(feeder, loadPort, loadPortTransferredPorts[i], transferred, transferredPorts[j], out pick)) != 0) return ret;
                    jobs.Add(pick);
                }
            }
            return ret;
        }
        protected virtual int GetPlaceJobs(Feeder transferable, LoadPort loadPort, Int32Collection loadPortTransferredPorts, ITransferred transferred, Int32Collection transferredPorts, out JobCollection jobs)
        {
            int ret = 0;
            Job job;
            jobs = new JobCollection();

            for (int i = 0; i < loadPortTransferredPorts.Count; i++)
            {
                for (int j = 0; j < transferredPorts.Count; j++)
                {
                    //if((ret = this.GetPlaceJob(transferable, loadPort, loadPortTransferredPorts[i], transferred, transferredPorts[j], out job)) != 0) return ret;
                    if((ret = this.GetPlaceJobWithPickSubJob(transferable, loadPort, loadPortTransferredPorts[i], transferred, transferredPorts[j], out job)) != 0) return ret;
                    jobs.Add(job);
                }
            }
            return ret;
        }
        protected virtual int GetPickJob(Feeder feeder, LoadPort loadPort, int loadPortTransferredPort, ITransferred transferred, int transferredPort, out Job job)
        {
            int ret = 0;
            FeederTransferJob feederTransferJob;
            JobPriority jobPriority;
            job = new TransferJob();
            bool tactEnable = false;

            string name = String.Format("Pick [{0},{1} <- {2}{3}]",
                transferred.Name, transferred.Ports[transferredPort].Name,
                loadPort.Name, loadPort.Ports[loadPortTransferredPort].Name);

            jobPriority = new JobPriority(GeneralJobDispatcherCreator.DefaultPickJobPriority, 0);

            #region FeederJob
            feederTransferJob = new FeederTransferJob(name, new FeederTransferJobCalculator(), new FeederTransferJobAction());
            jobPriority = new JobPriority(GeneralJobDispatcherCreator.DefaultLoadPortPickJobPriority, 0);
            tactEnable = true;

            feederTransferJob.Primary = feeder;
            feederTransferJob.PrimaryPorts.Add(0);
            feederTransferJob.Secondary = loadPort;
            feederTransferJob.SecondaryPorts.Add(loadPortTransferredPort);
            feederTransferJob.RelatedTransferredModule = transferred as TransferredModule;
            feederTransferJob.RelatedTransferredModulePort = transferredPort;

            feederTransferJob.TransferDirection = TransferDirection.Receive;
            feederTransferJob.JobCalculator.DefaultPriority = jobPriority;
            feederTransferJob.TackEnabled = tactEnable;
            feederTransferJob.RelatedTransferredModule = transferred as TransferredModule;

            jobPriority = new JobPriority(GeneralJobDispatcherCreator.DefaultPickJobPriority, 0);
            feederTransferJob.FeederToTransferredModuleTransferJob.Primary = feeder;
            feederTransferJob.FeederToTransferredModuleTransferJob.PrimaryPorts.Add(0);
            feederTransferJob.FeederToTransferredModuleTransferJob.Secondary = transferred;
            feederTransferJob.FeederToTransferredModuleTransferJob.SecondaryPorts.Add(transferredPort);
            feederTransferJob.FeederToTransferredModuleTransferJob.TransferDirection = TransferDirection.Send;
            feederTransferJob.FeederToTransferredModuleTransferJob.JobCalculator.DefaultPriority = jobPriority;
            feederTransferJob.FeederToTransferredModuleTransferJob.TackEnabled = tactEnable;

            job = feederTransferJob;
            #endregion


            return ret;
        }
        protected virtual int GetPlaceJob(Feeder feeder, LoadPort loadPort, int loadPortTransferredPort, ITransferred transferred, int transferredPort, out Job job)
        {
            int ret = 0;
            FeederTransferJob feederTransferJob;
            JobPriority jobPriority = new JobPriority(GeneralJobDispatcherCreator.DefaultPlaceJobPriority, 0);

            job = null;

            string name = string.Format("Place [{0},{1} -> {2}{3}]",
                transferred.Name, transferred.Ports[transferredPort].Name,
                loadPort.Name, loadPort.Ports[loadPortTransferredPort].Name);

            #region FeederJob
            //2018.12.24 Place 할때는 TransferJobCalculator 사용한다.
            feederTransferJob = new FeederTransferJob(name, new FeederTransferJobCalculator(), new FeederTransferJobAction());
            jobPriority = new JobPriority(GeneralJobDispatcherCreator.DefaultLoadPortPlaceJobPriority, 0);

            feederTransferJob.Primary = feeder;
            feederTransferJob.PrimaryPorts.Add(0);
            feederTransferJob.Secondary = loadPort;
            feederTransferJob.SecondaryPorts.Add(loadPortTransferredPort);
            feederTransferJob.RelatedTransferredModule = transferred as TransferredModule;
            feederTransferJob.RelatedTransferredModulePort = transferredPort;

            feederTransferJob.TransferDirection = TransferDirection.Send;
            feederTransferJob.JobCalculator.DefaultPriority = jobPriority;
            feederTransferJob.TackEnabled = true;
            feederTransferJob.RelatedTransferredModule = transferred as TransferredModule;

            jobPriority = new JobPriority(GeneralJobDispatcherCreator.DefaultPlaceJobPriority, 0);
            feederTransferJob.FeederToTransferredModuleTransferJob.Primary = feeder;
            feederTransferJob.FeederToTransferredModuleTransferJob.PrimaryPorts.Add(0);
            feederTransferJob.FeederToTransferredModuleTransferJob.Secondary = transferred;
            feederTransferJob.FeederToTransferredModuleTransferJob.SecondaryPorts.Add(transferredPort);
            feederTransferJob.FeederToTransferredModuleTransferJob.TransferDirection = TransferDirection.Receive;
            feederTransferJob.FeederToTransferredModuleTransferJob.JobCalculator.DefaultPriority = jobPriority;
            feederTransferJob.FeederToTransferredModuleTransferJob.TackEnabled = true;

            job = feederTransferJob;
            #endregion

            return ret;
        }
        protected virtual int GetPlaceJobWithPickSubJob(Feeder feeder, LoadPort loadPort, int loadPortTransferredPort, ITransferred transferred, int transferredPort, out Job job)
        {
            int ret = 0;
            FeederTransferJob feederTransferJob;
            string name = "";
            JobPriority jobPriority = new JobPriority(GeneralJobDispatcherCreator.DefaultPlaceJobPriority, 0);

            job = null;

            name = string.Format("Place [{0},{1} -> {2}{3}]",
                transferred.Name, transferred.Ports[transferredPort].Name,
                loadPort.Name, loadPort.Ports[loadPortTransferredPort].Name);

            #region FeederPlace main Job
            //2018.12.24 Place 할때는 TransferJobCalculator 사용한다.
            feederTransferJob = new FeederTransferJob(name, new FeederTransferJobCalculator(), new FeederTransferJobAction());
            jobPriority = new JobPriority(GeneralJobDispatcherCreator.DefaultLoadPortPlaceJobPriority, 0);

            feederTransferJob.Primary = feeder;
            feederTransferJob.PrimaryPorts.Add(0);
            feederTransferJob.Secondary = loadPort;
            feederTransferJob.SecondaryPorts.Add(loadPortTransferredPort);
            feederTransferJob.RelatedTransferredModule = transferred as TransferredModule;
            feederTransferJob.RelatedTransferredModulePort = transferredPort;

            feederTransferJob.TransferDirection = TransferDirection.Send;
            feederTransferJob.JobCalculator.DefaultPriority = jobPriority;
            feederTransferJob.TackEnabled = true;
            feederTransferJob.RelatedTransferredModule = transferred as TransferredModule;

            jobPriority = new JobPriority(GeneralJobDispatcherCreator.DefaultPlaceJobPriority, 0);
            feederTransferJob.FeederToTransferredModuleTransferJob.Primary = feeder;
            feederTransferJob.FeederToTransferredModuleTransferJob.PrimaryPorts.Add(0);
            feederTransferJob.FeederToTransferredModuleTransferJob.Secondary = transferred;
            feederTransferJob.FeederToTransferredModuleTransferJob.SecondaryPorts.Add(transferredPort);
            feederTransferJob.FeederToTransferredModuleTransferJob.TransferDirection = TransferDirection.Receive;
            feederTransferJob.FeederToTransferredModuleTransferJob.JobCalculator.DefaultPriority = jobPriority;
            feederTransferJob.FeederToTransferredModuleTransferJob.TackEnabled = true;

            job = feederTransferJob;
            #endregion

            #region Feeder Pick Sub Job
            FeederTransferJob subJob;
            bool tactEnable = false;

            name = string.Format("Pick [{0},{1} <- {2}{3}]",
                transferred.Name, transferred.Ports[transferredPort].Name,
                loadPort.Name, loadPort.Ports[loadPortTransferredPort].Name);

            jobPriority = new JobPriority(GeneralJobDispatcherCreator.DefaultPickJobPriority, 0);

            subJob = new FeederTransferJob(name, new FeederTransferJobCalculator(), new FeederTransferJobAction());
            jobPriority = new JobPriority(GeneralJobDispatcherCreator.DefaultLoadPortPickJobPriority, 0);
            tactEnable = true;

            subJob.Primary = feeder;
            subJob.PrimaryPorts.Add(0);
            subJob.Secondary = loadPort;
            subJob.SecondaryPorts.Add(loadPortTransferredPort);
            subJob.RelatedTransferredModule = transferred as TransferredModule;
            subJob.RelatedTransferredModulePort = transferredPort;

            subJob.TransferDirection = TransferDirection.Receive;
            subJob.JobCalculator.DefaultPriority = jobPriority;
            subJob.TackEnabled = tactEnable;
            subJob.RelatedTransferredModule = transferred as TransferredModule;

            jobPriority = new JobPriority(GeneralJobDispatcherCreator.DefaultPickJobPriority, 0);
            subJob.FeederToTransferredModuleTransferJob.Primary = feeder;
            subJob.FeederToTransferredModuleTransferJob.PrimaryPorts.Add(0);
            subJob.FeederToTransferredModuleTransferJob.Secondary = transferred;
            subJob.FeederToTransferredModuleTransferJob.SecondaryPorts.Add(transferredPort);
            subJob.FeederToTransferredModuleTransferJob.TransferDirection = TransferDirection.Send;
            subJob.FeederToTransferredModuleTransferJob.JobCalculator.DefaultPriority = jobPriority;
            subJob.FeederToTransferredModuleTransferJob.TackEnabled = tactEnable;

            job.Jobs.Add(subJob);
            #endregion

            #region Feeder retract sub job
            MotionFeederArmRetractJob retractJob;
            name = $"Retract [{feeder}]";
            retractJob = new MotionFeederArmRetractJob(name, new MotionFeederArmRetractJobCalculator(), new MotionFeederArmRetractJobAction());
            retractJob.TransferModule = feeder;
            job.Jobs.Add(retractJob);
            #endregion
            return ret;
        }
        
        #region get dispatcher
        protected virtual int GetTransferJobDispatcher(TransferableModule transferable, out JobDispatcher dispatcher)
        {
            int ret = 0;
            JobCollection jobs;
            ITransferred transferred;
            ISlotMapper slotMapper;

            #region TransferableModule이 SlotMapper를 제공하는지 체크한다.
            slotMapper = transferable as ISlotMapper;
            if (slotMapper == null && transferable is IHaveSlotMapper)
            {
                slotMapper = ((IHaveSlotMapper)transferable).SlotMapper;
            }
            #endregion

            dispatcher = new JobDispatcher();
            dispatcher.Concurrent = false;
            dispatcher.Name = string.Format(string.Format("JobDispatcher [{0}]", transferable.Name));

            if (transferable is TransferBasicFeeder feeder)
            {
                if ((ret = this.GetTransferJobDispatcher(feeder, ref dispatcher)) != 0) return ret;
            }
            else
            {
                // TransferItem의 개수만큼
                for (int i = 0; i < transferable.TransferItems.Count; i++)
                {
                    transferred = Sys.Equipment.Modules.GetByTypeAndName<ITransferred>(transferable.TransferItems[i].TransferredModule);
                    if (transferred == null)
                        throw new ArgumentException(string.Format("The transferred module [{0}] is not found.", transferable.TransferItems[i].TransferredModule));

                    // TransferJob
                    if ((ret = this.GetTransferJobs(transferable, transferable.TransferItems[i], out jobs)) != 0) return ret;
                    dispatcher.Jobs.AddRange(jobs);
                }
            }

            return ret;
        }

        protected virtual int GetRunRecipeJobDispatcher(out JobDispatcher dispatcher)
        {
            int ret = 0;
            Job job;

            dispatcher = new JobDispatcher();
            dispatcher.Concurrent = true;
            dispatcher.Name = string.Format("JobDispatcher [RunRecipe]");

            Module[] modules = Sys.Equipment.Modules.GetByType(typeof(ProcessModule));
            for (int i = 0; i < modules.Length; i++)
            {
                if ((ret = this.GetRunRecipeJob(modules[i] as ProcessModule, out job)) != 0) return ret;
                dispatcher.Jobs.Add(job);
            }

            return ret;
        }

        protected virtual int GetLoadPortSelfMappingJobDispatcher(out JobDispatcher dispatcher)
        {
            int ret = 0;
            Job job = null;
            LoadPort[] loadports = LoadPortManager.GetLoadPorts();

            dispatcher = null;

            for (int i = 0; i < loadports.Length; i++)
            {
                if ((loadports[i] is ISlotMapper) == false && ((loadports[i] is IHaveSlotMapper) == false || ((IHaveSlotMapper)loadports[i]).SlotMapper == null)) continue;

                if (dispatcher == null)
                {
                    dispatcher = new JobDispatcher();
                    dispatcher.Concurrent = true;
                    dispatcher.Name = "JobDispatcher [LoadPortSelfMapping]";
                }

                foreach (LoadPortPlate plate in loadports[i].Plates)
                {
                    int loadportPlateIndex = loadports[i].Plates.IndexOf(plate);
                    if ((ret = this.GetLoadPortSelfMappingJob(loadports[i], loadportPlateIndex, out job)) != 0) return ret;
                    if (job != null)
                        dispatcher.Jobs.Add(job);
                }
            }

            return ret;
        }

        protected int GetSelfExecutionJobDispatcher(out JobDispatcher dispatcher)
        {
            int ret = 0;
            ISupportJob[] supportJobs = null;
            JobCollection jobs = null;

            supportJobs = Sys.Equipment.Modules.GetByType<ISupportJob>();
            dispatcher = null;
            if (supportJobs == null) return ret;

            dispatcher = new JobDispatcher();
            dispatcher.Concurrent = true;
            dispatcher.Name = "JobDispatcher [Self-Execution]";

            foreach (ISupportJob supportJob in supportJobs)
            {
                jobs = supportJob.GetJobs();
                foreach (Job job in jobs)
                    dispatcher.Jobs.Add(job);
            }

            return ret;
        }
        #endregion

        #region get job
        protected virtual int GetTransferJobs(TransferableModule transferable, out JobCollection jobs)
        {
            int ret = 0;
            JobCollection transferJobs;

            jobs = new JobCollection();

            for (int i = 0; i < transferable.TransferItems.Count; i++)
            {
                if ((ret = this.GetTransferJobs(transferable, transferable.TransferItems[i], out transferJobs)) != 0) return ret;
                jobs.Add(transferJobs);
            }

            return ret;
        }

        protected virtual int GetTransferJobs(TransferableModule transferable, TransferItem transferItem, out JobCollection jobs)
        {
            int ret = 0;
            Int32Collection transferablePorts, transferredPorts;
            JobCollection pickJobs, placeJobs, pickAndPlaceJobs;
            ITransferred transferred = Sys.Equipment.Modules.GetByName(transferItem.TransferredModule) as ITransferred;

            jobs = new JobCollection();

            #region get ports
            // 설정되지 않은 경우는 전체 port
            if (0 < transferItem.TransferablePorts.Count)
                transferablePorts = transferItem.TransferablePorts;
            else
                transferablePorts = transferable.Ports.GetPortIndexes();

            // 설정되지 않은 경우는 전체 port
            if (0 < transferItem.TransferredPorts.Count)
                transferredPorts = transferItem.TransferredPorts;
            else
                transferredPorts = transferred.Ports.GetPortIndexes();
            #endregion

            // pick
            if ((ret = this.GetPickJobs(transferable, transferablePorts, transferred, transferredPorts, out pickJobs)) != 0) return ret;
            jobs.Add(pickJobs);

            // place
            if ((ret = this.GetPlaceJobs(transferable, transferablePorts, transferred, transferredPorts, out placeJobs)) != 0) return ret;
            jobs.Add(placeJobs);

            // PickAndPlace Jobs
            if ((ret = this.GetPickAndPlaceJobs(transferable, transferablePorts, transferred, transferredPorts, out pickAndPlaceJobs)) != 0) return ret;
            jobs.Add(pickAndPlaceJobs);

            return ret;
        }

        protected virtual int GetPickJobs(TransferableModule transferable, Int32Collection transferablePorts, ITransferred transferred, Int32Collection transferredPorts, out JobCollection jobs)
        {
            int ret = 0;
            Job pick = null, place = null, ready = null;

            jobs = new JobCollection();

            for (int i = 0; i < transferablePorts.Count; i++)
            {
                for (int j = 0; j < transferredPorts.Count; j++)
                {
                    if ((ret = this.GetPickJob(transferable, transferablePorts[i], transferred, transferredPorts[j], out pick)) != 0) return ret;
                    jobs.Add(pick);
                    // Revision 6
                    for (int k = 0; k < transferablePorts.Count; k++)
                    {
                        if (i == k) continue;
                        if (transferred is LoadPort) continue; // for ConcurrentProcessNotIntersectionPath
                        if ((ret = this.GetPlaceJob(transferable, transferablePorts[k], transferred, transferredPorts[j], out place)) != 0) return ret;
                        pick.Jobs.Add(place);
                    }

                    if (transferred is ProcessModule && this.ConstructConfiguration.EnableReadyToTransferJob == true)
                    {
                        if ((ret = this.GetPickReadyToTransferJob(transferable, transferablePorts[i], transferred, transferredPorts[j], out ready)) != 0) return ret;
                        if (ready != null)
                            jobs.Add(ready);
                    }
                }
            }

            return ret;
        }

        protected virtual int GetPickJob(TransferableModule transferable, int transferablePort, ITransferred transferred, int transferredPort, out Job job)
        {
            int ret = 0;
            TransferJob transferJob;
            JobPriority jobPriority;
            bool tactEnabled = false;

            job = null;

            string name = string.Format("Pick [{0},{1} <- {2},{3}]",
                transferable.Name, transferable.Ports[transferablePort].Name,
                transferred.Name, transferred.Ports[transferredPort].Name);

            jobPriority = new JobPriority(GeneralJobDispatcherCreator.DefaultPickJobPriority, 0);

            if (transferred is LoadPort)
            {
                transferJob = new TransferJob(name, new GeneralSubstrateInCarrierTransferJobCalculator(), new SubstrateInCarrierTransferJobAction());
                jobPriority = new JobPriority(GeneralJobDispatcherCreator.DefaultLoadPortPickJobPriority, 0);
                tactEnabled = true;
            }
            else if (transferred is ITransferable)
            {
                transferJob = new TransferJob(name, new TransferJobCalculator(), new TransferJobAction());
                jobPriority = new JobPriority(GeneralJobDispatcherCreator.DefaultTransferablePickJobPriority, 0);
                tactEnabled = false;
            }
            else
            {
                transferJob = new TransferJob(name, new TransferJobCalculator(), new TransferJobAction());
                tactEnabled = true;
            }

            transferJob.Primary = transferable;
            transferJob.PrimaryPorts.Add(transferablePort);
            transferJob.Secondary = transferred;
            transferJob.SecondaryPorts.Add(transferredPort);
            transferJob.TransferDirection = TransferDirection.Receive;
            transferJob.JobCalculator.DefaultPriority = jobPriority;
            transferJob.TackEnabled = tactEnabled;

            if (transferred is ProcessModule == true)
            {
                TransferableModule[] transferables = TransferredModule.GetAccessibleModules(transferred.Name);
                if (1 < transferables.Length)
                    transferJob.EnableOvertaking = true;
                else
                    transferJob.EnableOvertaking = false;
            }
            else
                transferJob.EnableOvertaking = true;

            job = transferJob;

            return ret;
        }

        /// <summary>
        /// n개의 port를 이용하여 동시에 pick하는 job을 얻는다.
        /// </summary>
        /// <param name="transferable"></param>
        /// <param name="transferablePorts"></param>
        /// <param name="transferred"></param>
        /// <param name="transferredPorts"></param>
        /// <param name="job"></param>
        /// <returns></returns>
        protected virtual int GetPickJob(TransferableModule transferable, IList<int> transferablePorts, ITransferred transferred, IList<int> transferredPorts, out Job job)
        {
            int ret = 0;
            string name = "";
            TransferJob transferJob;

            job = null;

            if ((ret = this.GetPickJob(transferable, transferablePorts[0], transferred, transferredPorts[0], out job)) != 0) return ret;

            #region make name "Pick [robot,(arm1,arm2) <- module,(slot1,slot2)]
            name = string.Format("Pick [{0},(", transferable.Name);
            for (int i = 0; i < transferablePorts.Count; i++)
            {
                name += transferable.Ports[transferablePorts[i]].Name;
                if (i == transferablePorts.Count - 1)
                    name += ") <- ";
                else
                    name += ",";
            }
            name += transferred.Name + ",(";
            for (int i = 0; i < transferredPorts.Count; i++)
            {
                name += transferred.Ports[transferredPorts[i]].Name;
                if (i == transferredPorts.Count - 1)
                    name += ")]";
                else
                    name += ",";
            }
            #endregion

            transferJob = job as TransferJob;
            transferJob.Name = name;
            for (int i = 1; i < transferablePorts.Count; i++)
                transferJob.PrimaryPorts.Add(transferablePorts[i]);
            for (int i = 1; i < transferredPorts.Count; i++)
                transferJob.SecondaryPorts.Add(transferredPorts[i]);
            transferJob.JobCalculator.DefaultPriority = new JobPriority(transferJob.JobCalculator.DefaultPriority.Major + 1, transferJob.JobCalculator.DefaultPriority.Minor);

            return ret;
        }

        protected virtual int GetPickReadyToTransferJob(TransferableModule transferable, int transferablePort, ITransferred transferred, int transferredPort, out Job job)
        {
            int ret = 0;
            ReadyToTransferJob readyJob;
            JobPriority jobPriority = new JobPriority(GeneralJobDispatcherCreator.DefaultPickReadyToTransferJobPriority, 0);

            job = null;

            string name = string.Format("ReadyToTransfer [{0},{1} <- {2},{3}]",
                transferable.Name, transferable.Ports[transferablePort].Name,
                transferred.Name, transferred.Ports[transferredPort].Name);

            readyJob = new ReadyToTransferJob(name, new ReadyToTransferJobCalculator(), new ReadyToTransferJobAction());

            readyJob.Primary = transferable;
            readyJob.PrimaryPorts.Add(transferablePort);
            readyJob.Secondary = transferred;
            readyJob.SecondaryPorts.Add(transferredPort);
            readyJob.TransferDirection = TransferDirection.Receive;
            readyJob.JobCalculator.DefaultPriority = jobPriority;

            if (transferred is ProcessModule == true)
                readyJob.EnableOvertaking = false;   // 검증되지 않았다
            else
                readyJob.EnableOvertaking = true;

            job = readyJob;

            return ret;
        }

        protected virtual int GetPlaceJobs(TransferableModule transferable, Int32Collection transferablePorts, ITransferred transferred, Int32Collection transferredPorts, out JobCollection jobs)
        {
            int ret = 0;
            Job job;
            jobs = new JobCollection();

            for (int i = 0; i < transferablePorts.Count; i++)
            {
                for (int j = 0; j < transferredPorts.Count; j++)
                {
                    if ((ret = this.GetPlaceJob(
                        transferable, transferablePorts[i], transferred, transferredPorts[j], out job)) != 0) return ret;
                    jobs.Add(job);
                }
            }

            return ret;
        }

        protected virtual int GetPlaceJob(TransferableModule transferable, int transferablePort, ITransferred transferred, int transferredPort, out Job job)
        {
            int ret = 0;
            TransferJob transferJob;
            JobPriority jobPriority = new JobPriority(GeneralJobDispatcherCreator.DefaultPlaceJobPriority, 0);
            job = null;

            string name = string.Format("Place [{0},{1} -> {2},{3}]",
                transferable.Name, transferable.Ports[transferablePort].Name,
                transferred.Name, transferred.Ports[transferredPort].Name);

            if (transferred is LoadPort)
            {
                transferJob = new TransferJob(name, new SubstrateInCarrierTransferJobCalculator(), new SubstrateInCarrierTransferJobAction());
                jobPriority = new JobPriority(GeneralJobDispatcherCreator.DefaultLoadPortPlaceJobPriority, 0);
            }
            else
                transferJob = new TransferJob(name, new TransferJobCalculator(), new TransferJobAction());

            transferJob.Primary = transferable;
            transferJob.PrimaryPorts.Add(transferablePort);
            transferJob.Secondary = transferred;
            transferJob.SecondaryPorts.Add(transferredPort);
            transferJob.TransferDirection = TransferDirection.Send;
            transferJob.JobCalculator.DefaultPriority = jobPriority;
            // Revision 4
            transferJob.TackEnabled = true;

            job = transferJob;

            return ret;
        }

        protected virtual int GetPlaceJob(TransferableModule transferable, IList<int> transferablePorts, ITransferred transferred, IList<int> transferredPorts, out Job job)
        {
            int ret = 0;
            string name = "";
            TransferJob transferJob;

            job = null;

            if ((ret = this.GetPlaceJob(transferable, transferablePorts[0], transferred, transferredPorts[0], out job)) != 0) return ret;

            #region make name "Place [robot,(arm1,arm2) -> module,(slot1,slot2)]
            name = string.Format("Place [{0},(", transferable.Name);
            for (int i = 0; i < transferablePorts.Count; i++)
            {
                name += transferable.Ports[transferablePorts[i]].Name;
                if (i == transferablePorts.Count - 1)
                    name += ") -> ";
                else
                    name += ",";
            }
            name += transferred.Name + ",(";
            for (int i = 0; i < transferredPorts.Count; i++)
            {
                name += transferred.Ports[transferredPorts[i]].Name;
                if (i == transferredPorts.Count - 1)
                    name += ")]";
                else
                    name += ",";
            }
            #endregion

            transferJob = job as TransferJob;
            transferJob.Name = name;
            for (int i = 1; i < transferablePorts.Count; i++)
                transferJob.PrimaryPorts.Add(transferablePorts[i]);
            for (int i = 1; i < transferredPorts.Count; i++)
                transferJob.SecondaryPorts.Add(transferredPorts[i]);
            transferJob.JobCalculator.DefaultPriority = new JobPriority(transferJob.JobCalculator.DefaultPriority.Major + 1, transferJob.JobCalculator.DefaultPriority.Minor);

            return ret;
        }

        protected virtual int GetPickAndPlaceJobs(TransferableModule transferable, Int32Collection transferablePorts, ITransferred transferred, Int32Collection transferredPorts, out JobCollection jobs)
        {
            int ret = 0;
            Job job;
            IPickAndPlaceTransferRobot pickAndPlaceTransferRobot = null;
            IPickAndPlaceTransferred pickAndPlaceTransferred = null;
            int pickPort = 0, placePort = 0;

            jobs = new JobCollection();

            if (transferablePorts.Count < 2) return ret;

            // added by biglake 2016/11/08
            pickAndPlaceTransferRobot = transferable as IPickAndPlaceTransferRobot;
            if (pickAndPlaceTransferRobot == null || pickAndPlaceTransferRobot.EnablePickAndPlace == false) return ret;

            // added by biglake 2016/11/08
            pickAndPlaceTransferred = transferred as IPickAndPlaceTransferred;
            if (pickAndPlaceTransferred == null || pickAndPlaceTransferred.EnablePickAndPlace == false) return ret;

            for (int i = 0; i < transferablePorts.Count; i++)
            {
                pickPort = transferablePorts[i];

                for (int j = 0; j < transferablePorts.Count; j++)
                {
                    placePort = transferablePorts[j];
                    if (pickPort == placePort) continue;
                    for (int k = 0; k < transferredPorts.Count; k++)
                    {
                        if ((ret = this.GetPickAndPlaceJob(transferable, pickPort, placePort, transferred, transferredPorts[k], out job)) != 0) return ret;
                        if (job != null) jobs.Add(job);
                    }
                }
            }

            return ret;
        }

        protected virtual int GetPickAndPlaceJob(TransferableModule transferable, int pickPort, int placePort, ITransferred transferred, int transferredPort, out Job job)
        {
            int ret = 0;
            TransferJob transferJob;
            JobPriority jobPriority = new JobPriority(GeneralJobDispatcherCreator.DefaultPickAndPlaceJobPriority, 0);
            string name = "";

            job = null;

            name = string.Format("PickAndPlace [{0},{1},{2} <-> {3},{4}]",
                transferable.Name, transferable.Ports[pickPort].Name, transferable.Ports[placePort].Name,
                transferred.Name, transferred.Ports[transferredPort].Name);

            transferJob = new TransferJob(name, new TransferJobCalculator(), new TransferJobAction());

            transferJob.Primary = transferable;
            transferJob.PrimaryPorts.Add(pickPort);
            transferJob.Secondary = transferred;
            transferJob.SecondaryPorts.Add(transferredPort);

            transferJob.SecondActionPrimaryPorts.Add(placePort);
            transferJob.SecondActionSecondaryPorts.Add(transferredPort);

            transferJob.TransferDirection = TransferDirection.ReceiveAndSend;
            transferJob.JobCalculator.DefaultPriority = jobPriority;

            // Revision 4
            transferJob.TackEnabled = true;
            // Revision 5
            if (transferred is ITransferable)
                transferJob.TackEnabled = false;

            if (transferred is ProcessModule == true)
                transferJob.EnableOvertaking = false;
            else
                transferJob.EnableOvertaking = true;

            job = transferJob;

            return ret;
        }

        protected virtual int GetLoadPortSelfMappingJob(LoadPort loadport, int loadportPlateIndex, out Job job)
        {
            int ret = 0;

            job = null;

            string name = string.Format("SelfMapping [{0}], Plate[{1}]", loadport.Name, loadportPlateIndex);
            LoadPortSelfMappingJob mappingJob = new LoadPortSelfMappingJob(name, new GemLoadPortSelfMappingJobCalculator(), new LoadPortSelfMappingJobAction());
            mappingJob.LoadPort = loadport;
            mappingJob.LoadPortPlateIndex = loadportPlateIndex;
            mappingJob.JobCalculator.DefaultPriority = new JobPriority(GeneralJobDispatcherCreator.DefaultSelfMappingJobPriority, 0);
            mappingJob.CheckingJobOrder = true;

            job = mappingJob;

            return ret;
        }

        protected virtual int GetRunRecipeJob(ProcessModule processModule, out Job job)
        {
            int ret = 0;
            job = null;

            string name = string.Format("RunRecipe [{0}]", processModule.Name);
            RunRecipeJob runRecipeJob = new RunRecipeJob(name, new RunRecipeJobCalculator(), new RunRecipeJobAction());
            runRecipeJob.Module = processModule;
            runRecipeJob.JobCalculator.DefaultPriority = new JobPriority(GeneralJobDispatcherCreator.DefaultRunRecipeJobPriority, 0);

            job = runRecipeJob;

            return ret;
        }

        protected virtual int CreateTransferVsSelfExecutionJobDispatcherJudge(JobDispatcherCollection transferDispatchers, JobDispatcherCollection selfExecutionDispatchers, out TransferVsSelfExecutionJobDispatcherJudge jobDispatcherJudge)
        {
            int ret = 0;

            jobDispatcherJudge = null;

            return ret;
        }
        #endregion

        #endregion

        #region JobDispatcherCreator Members
        protected override int OnCreateJobDispatcher()
        {
            int ret = 0;
            JobDispatcher dispatcher = null;
            TransferableModule[] transferableModules = null;
            JobDispatcherCollection transferDispatchers = new JobDispatcherCollection();
            JobDispatcherCollection selfExecutionDispatchers = new JobDispatcherCollection();
            TransferVsSelfExecutionJobDispatcherJudge judge = null;

            #region load port self mapping
            if ((ret = this.GetLoadPortSelfMappingJobDispatcher(out dispatcher)) != 0) return ret;
            if (dispatcher != null && 0 < dispatcher.Jobs.Count)
                JobDispatcherManager.Dispatchers.Add(dispatcher);
            #endregion

            #region transfer
            transferableModules = Sys.Equipment.Modules.GetByType<TransferableModule>();
            for (int i = 0; i < transferableModules.Length; i++)
            {
                if ((ret = this.GetTransferJobDispatcher(transferableModules[i], out dispatcher)) != 0) return ret;
                if (dispatcher != null && 0 < dispatcher.Jobs.Count)
                {
                    JobDispatcherManager.Dispatchers.Add(dispatcher);
                    transferDispatchers.Add(dispatcher);
                }
            }
            #endregion

            #region run recipe
            if ((ret = this.GetRunRecipeJobDispatcher(out dispatcher)) != 0) return ret;
            if (dispatcher != null && 0 < dispatcher.Jobs.Count)
                JobDispatcherManager.Dispatchers.Add(dispatcher);
            #endregion

            #region self-execution
            if ((ret = this.GetSelfExecutionJobDispatcher(out dispatcher)) != 0) return ret;
            if (dispatcher != null && 0 < dispatcher.Jobs.Count)
            {
                JobDispatcherManager.Dispatchers.Add(dispatcher);
                selfExecutionDispatchers.Add(dispatcher);
            }
            #endregion

            #region create transfer vs self-execution job dispatcher judge
            if ((ret = this.CreateTransferVsSelfExecutionJobDispatcherJudge(transferDispatchers, selfExecutionDispatchers, out judge)) != 0) return ret;
            if (judge != null)
            {
                for (int j = 0; j < judge.Dispatchers.Count; j++)
                {
                    if (JobDispatcherManager.Dispatchers.Contains(judge.Dispatchers[j]) == true)
                        JobDispatcherManager.Dispatchers.Remove(judge.Dispatchers[j]);
                }
                JobDispatcherManager.Judges.Add(judge);
            }
            #endregion

            return ret;
        }
        #endregion

        #region Element Members
        public new GeneralSemiEquipment Owner
        {
            get { return base.Owner as GeneralSemiEquipment; }
        }

        protected new GeneralJobDispatcherCreatorConstructConfiguration ConstructConfiguration
        {
            get { return base.ConstructConfiguration as GeneralJobDispatcherCreatorConstructConfiguration; }
        }

        protected override ElementConstructConfiguration OnGetDefaultConstructConfiguration()
        {
            return new GeneralJobDispatcherCreatorConstructConfiguration();
        }

        protected override void OnSetConstructConfiguration(ElementConstructConfiguration configuration)
        {
            base.OnSetConstructConfiguration(configuration);

            if (this.ConstructConfiguration == null) return;
        }
        #endregion
    }
    #endregion

    #region GeneralJobDispatcherCreatorConstructConfiguration
    [Serializable]
    public class GeneralJobDispatcherCreatorConstructConfiguration : JobDispatcherCreatorConstructConfiguration
    {
        #region Field
        private bool m_EnableReadyToTransferJob;
        #endregion

        #region Constructor
        public GeneralJobDispatcherCreatorConstructConfiguration(ElementConstructMethod constructMethod)
            : base(constructMethod)
        {
        }
        public GeneralJobDispatcherCreatorConstructConfiguration() : this(ElementConstructMethod.Static) { }
        #endregion

        #region Property
        [Category("JobDispatcherCreator")]
        [DefaultValue(false)]
        public bool EnableReadyToTransferJob
        {
            get { return this.m_EnableReadyToTransferJob; }
            set { this.m_EnableReadyToTransferJob = value; }
        }
        #endregion

        #region ConstructConfiguration Members
        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();

            this.EnableReadyToTransferJob = false;
        }
        #endregion
    }
    #endregion
}
