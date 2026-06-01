using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Secs;
using MechaSys.SoftBricks.Transfer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace QMC.Transfers
{
    [Serializable]
    public class RelatedTransferSpecification : TransferSpecification
    {
        #region Field
        private string m_RelatedTransferred;
        private Int32Collection m_RelatedTransferredPorts;
        #endregion

        #region Constructor
        public RelatedTransferSpecification() : base()
        {
            this.RelatedTransferred = string.Empty;
            this.RelatedTransferredPorts = new Int32Collection();
        }
        #endregion

        #region Property
        public string RelatedTransferred
        {
            get { return this.m_RelatedTransferred; }
            set { this.m_RelatedTransferred = value; }
        }
        
        public Int32Collection RelatedTransferredPorts
        {
            get { return this.m_RelatedTransferredPorts; }
            set { this.m_RelatedTransferredPorts = value; }
        }
        [SecsConvertible(false)]
        [XmlIgnore]
        public int RelatedTransferredPort
        {
            get { return this.m_RelatedTransferredPorts[0]; }
            set
            {
                if(1 <= this.m_RelatedTransferredPorts.Count)
                    this.m_RelatedTransferredPorts[0] = value;
                else
                    this.m_RelatedTransferredPorts.Add(value);
            }
        }
        #endregion

        #region Method
        public override void CopyTo(TransferSpecification target)
        {
            base.CopyTo(target);

            if(target is RelatedTransferSpecification specification)
            {
                specification.RelatedTransferred = this.RelatedTransferred;
                specification.RelatedTransferredPorts.Clear();
                for(int i = 0; i < this.RelatedTransferredPorts.Count; i++)
                    specification.RelatedTransferredPorts.Add(this.RelatedTransferredPorts[i]);
            }
        }

        public new RelatedTransferSpecification GetOppositeSpecification()
        {
            RelatedTransferSpecification specification = new RelatedTransferSpecification();

            this.CopyTo(specification);

            // Revision 3
            //specification.Direction = this.Direction == TransferDirection.Receive ? TransferDirection.Send : TransferDirection.Receive;
            if(this.Direction == TransferDirection.Receive)
                specification.Direction = TransferDirection.Send;
            else if(this.Direction == TransferDirection.Send)
                specification.Direction = TransferDirection.Receive;
            else if(this.Direction == TransferDirection.ReceiveAndSend)
                specification.Direction = TransferDirection.SendAndReceive;
            else
                specification.Direction = TransferDirection.ReceiveAndSend;

            specification.Role = this.Role == TransferRole.Primary ? TransferRole.Secondary : TransferRole.Primary;

            return specification;
        }
        #endregion
    }
}
