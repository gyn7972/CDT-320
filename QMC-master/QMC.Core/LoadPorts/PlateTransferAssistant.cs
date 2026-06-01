using System;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.Diagnostics;
using MechaSys.SoftBricks.Exceptions;
using MechaSys.SoftBricks.Transfer;

namespace QMC.LoadPorts
{
    #region PlateTransferAssistant
    public abstract class PlateTransferAssistant : OperationalElement
	{
		#region Define
		private delegate int MoveToTrasnferDelegate(TransferDirection direction, int plateIndex, int carrierIndex, int slotIndex);
		private delegate int MoveToScanDelegate(int plateIndex, int carrierIndex);
		private delegate int MoveToReadyDelegate(int plateIndex, int carrierIndex);
        #endregion

        #region Field
        #endregion

        #region Constructor
        public PlateTransferAssistant(Nameable nameable)
			: base(nameable)
		{
		}
		public PlateTransferAssistant() : this(new Nameable()) { }
		#endregion

		#region Property
		#endregion

		#region Method
		#region MoveToTransfer()
		/// <summary>
		/// MoveToTransfer 비동기 작업을 수행합니다.
		/// </summary>
		/// <param name="plateIndex"></param>
		/// <param name="carrierIndex"></param>
		/// <param name="slotIndex"></param>
		/// <param name="callback">MethodCallerAsyncCallback 비동기 작업의 완료 알림을 받는 대리자입니다</param>
		/// <param name="state">비동기 작업과 관련된 상태 정보를 포함하는 응용프로그램에서 지정된 객체입니다.</param>
		/// <returns>MethodCallerAsyncResult는 비동기 작업을 참조합니다.</returns>
		public MethodCallerAsyncResult BeginMoveToTransfer(TransferDirection direction, int plateIndex, int carrierIndex, int slotIndex, MethodCallerAsyncCallback callback, object state)
		{
			return this.Operational.BeginInvoke(new MoveToTrasnferDelegate(this.MoveToTransferProcedure), new object[] { direction, plateIndex, carrierIndex, slotIndex }, callback, state);
		}
		/// <summary>
		/// MoveToTransfer 비동기 작업을 수행합니다.
		/// </summary>
		/// <returns>MethodCallerAsyncResult는 비동기 작업을 참조합니다.</returns>
		public MethodCallerAsyncResult BeginMoveToTransfer(TransferDirection direction, int plateIndex, int carrierIndex, int slotIndex)
		{
			return this.BeginMoveToTransfer(direction, plateIndex, carrierIndex, slotIndex, null, null);
		}
		/// <summary>
		/// MoveToTransfer 비동기 작업을 완료합니다.
		/// </summary>
		/// <param name="ar">BeginBlock 메서드를 호출하여 반환된 MethodCallerAsyncResult입니다.</param>
		/// <returns>작업에 대한 결과를 반환한다. 0이면 성공 그렇지 않으면 실패를 나타냅니다</returns>
		public int EndMoveToTransfer(MethodCallerAsyncResult ar)
		{
			return (int)this.Operational.EndInvoke(ar);
		}
		/// <summary>
		/// MoveToTransfer 작업을 수행합니다.
		/// </summary>
		/// <param name="plateIndex"></param>
		/// <param name="carrierIndex"></param>
		/// <param name="slotIndex"></param>
		/// <returns>작업에 대한 결과를 반환한다. 0이면 성공 그렇지 않으면 실패를 나타냅니다</returns>
		public int MoveToTransfer(TransferDirection direction, int plateIndex, int carrierIndex, int slotIndex)
		{
			this.Operational.Stop();
			return (int)this.MoveToTransferProcedure(direction, plateIndex, carrierIndex, slotIndex);
		}

		private int MoveToTransferProcedure(TransferDirection direction, int plateIndex, int carrierIndex, int slotIndex)
		{
			int ret = 0;
			if ((ret = this.OnMoveToTransfer(direction, plateIndex, carrierIndex, slotIndex)) != 0) return ret;
			return ret;
		}

		protected abstract int OnMoveToTransfer(TransferDirection direction, int plateIndex, int carrierIndex, int slotIndex);
		#endregion

		#region MoveToScan()
		/// <summary>
		/// MoveToScan 비동기 작업을 수행합니다.
		/// </summary>
		/// <param name="callback">MethodCallerAsyncCallback 비동기 작업의 완료 알림을 받는 대리자입니다</param>
		/// <param name="state">비동기 작업과 관련된 상태 정보를 포함하는 응용프로그램에서 지정된 객체입니다.</param>
		/// <returns>MethodCallerAsyncResult는 비동기 작업을 참조합니다.</returns>
		public MethodCallerAsyncResult BeginMoveToScan(int plateIndex, int carrierIndex, MethodCallerAsyncCallback callback, object state)
		{
			return this.Operational.BeginInvoke(new MoveToScanDelegate(this.MoveToScanProcedure), new object[] { plateIndex, carrierIndex }, callback, state);
		}
		/// <summary>
		/// MoveToScan 비동기 작업을 수행합니다.
		/// </summary>
		/// <returns>MethodCallerAsyncResult는 비동기 작업을 참조합니다.</returns>
		public MethodCallerAsyncResult BeginMoveToScan(int plateIndex, int carrierIndex)
		{
			return this.BeginMoveToScan(plateIndex, carrierIndex, null, null);
		}
		/// <summary>
		/// MoveToScan 비동기 작업을 완료합니다.
		/// </summary>
		/// <param name="ar">BeginBlock 메서드를 호출하여 반환된 MethodCallerAsyncResult입니다.</param>
		/// <returns>작업에 대한 결과를 반환한다. 0이면 성공 그렇지 않으면 실패를 나타냅니다</returns>
		public int EndMoveToScan(MethodCallerAsyncResult ar)
		{
			return (int)this.Operational.EndInvoke(ar);
		}
		/// <summary>
		/// MoveToScan 작업을 수행합니다.
		/// </summary>
		/// <returns>작업에 대한 결과를 반환한다. 0이면 성공 그렇지 않으면 실패를 나타냅니다</returns>
		public int MoveToScan(int plateIndex, int carrierIndex)
		{
			this.Operational.Stop();
			return (int)this.MoveToScanProcedure(plateIndex, carrierIndex);
		}

		private int MoveToScanProcedure(int plateIndex, int carrierIndex)
		{
			int ret = 0;
			if ((ret = this.OnMoveToScan(plateIndex, carrierIndex)) != 0) return ret;
			return ret;
		}

		protected abstract int OnMoveToScan(int plateIndex, int carrierIndex);
		#endregion

		#region MoveToReady()
		/// <summary>
		/// MoveToReady 비동기 작업을 수행합니다.
		/// </summary>
		/// <param name="callback">MethodCallerAsyncCallback 비동기 작업의 완료 알림을 받는 대리자입니다</param>
		/// <param name="state">비동기 작업과 관련된 상태 정보를 포함하는 응용프로그램에서 지정된 객체입니다.</param>
		/// <returns>MethodCallerAsyncResult는 비동기 작업을 참조합니다.</returns>
		public MethodCallerAsyncResult BeginMoveToReady(int plateIndex, int carrierIndex, MethodCallerAsyncCallback callback, object state)
		{
			return this.Operational.BeginInvoke(new MoveToReadyDelegate(this.MoveToReadyProcedure), new object[] { plateIndex, carrierIndex }, callback, state);
		}
		/// <summary>
		/// MoveToReady 비동기 작업을 수행합니다.
		/// </summary>
		/// <returns>MethodCallerAsyncResult는 비동기 작업을 참조합니다.</returns>
		public MethodCallerAsyncResult BeginMoveToReady(int plateIndex, int carrierIndex)
		{
			return this.BeginMoveToReady(plateIndex, carrierIndex, null, null);
		}
		/// <summary>
		/// MoveToReady 비동기 작업을 완료합니다.
		/// </summary>
		/// <param name="ar">BeginBlock 메서드를 호출하여 반환된 MethodCallerAsyncResult입니다.</param>
		/// <returns>작업에 대한 결과를 반환한다. 0이면 성공 그렇지 않으면 실패를 나타냅니다</returns>
		public int EndMoveToReady(MethodCallerAsyncResult ar)
		{
			return (int)this.Operational.EndInvoke(ar);
		}
		/// <summary>
		/// MoveToReady 작업을 수행합니다.
		/// </summary>
		/// <returns>작업에 대한 결과를 반환한다. 0이면 성공 그렇지 않으면 실패를 나타냅니다</returns>
		public int MoveToReady(int plateIndex, int carrierIndex)
		{
			this.Operational.Stop();
			return (int)this.MoveToReadyProcedure(plateIndex, carrierIndex);
		}

		private int MoveToReadyProcedure(int plateIndex, int carrierIndex)
		{
			int ret = 0;
			if ((ret = this.OnMoveToReady(plateIndex, carrierIndex)) != 0) return ret;
			return ret;
		}

		protected abstract int OnMoveToReady(int plateIndex, int carrierIndex);
		#endregion
		#endregion

		#region Element
		public new PlateTransferLoadPort Owner
		{
			get { return base.Owner as PlateTransferLoadPort; }
		}

		protected new PlateTransferAssistantConstructConfiguration ConstructConfiguration
		{
			get { return base.ConstructConfiguration as PlateTransferAssistantConstructConfiguration; }
		}

		protected override ElementConstructConfiguration OnGetDefaultConstructConfiguration()
		{
			return new PlateTransferAssistantConstructConfiguration();
		}

		protected override void OnSetConstructConfiguration(ElementConstructConfiguration configuration)
		{
			base.OnSetConstructConfiguration(configuration);

			if (this.ConstructConfiguration == null) return;
		}
		#endregion
	}
	#endregion

	#region PlateTransferAssistantConstructConfiguration
	[Serializable]
	public class PlateTransferAssistantConstructConfiguration : OperationalElementConstructConfiguration
	{
		#region Field
		#endregion

		#region Constructor
		public PlateTransferAssistantConstructConfiguration(ElementConstructMethod constructMethod)
			: base(ElementKind.Element, constructMethod)
		{
		}
		public PlateTransferAssistantConstructConfiguration() : this(ElementConstructMethod.Static) { }
		#endregion

		#region Property
		#endregion

		#region ConstructConfiguration
		protected override void SetDefaultValues()
		{
			base.SetDefaultValues();
		}
		#endregion
	}
	#endregion
}