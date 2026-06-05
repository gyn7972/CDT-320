using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Configurations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QMC.Parts.Assistant
{
    #region PartInitializeAssistant
    public abstract class PartInitializeAssistant : OperationalElement
	{
		#region Field
		#endregion

		#region Constructor
		public PartInitializeAssistant(Nameable nameable)
			: base(nameable)
		{
		}
		public PartInitializeAssistant() : this(new Nameable()) { }
		#endregion

		#region Property
		#endregion

		#region Method
		#region Initialize()
		/// <summary>
		/// Initialize 비동기 작업을 수행합니다.
		/// </summary>
		/// <param name="callback">MethodCallerAsyncCallback 비동기 작업의 완료 알림을 받는 대리자입니다</param>
		/// <param name="state">비동기 작업과 관련된 상태 정보를 포함하는 응용프로그램에서 지정된 객체입니다.</param>
		/// <returns>MethodCallerAsyncResult는 비동기 작업을 참조합니다.</returns>
		public MethodCallerAsyncResult BeginInitialize(MethodCallerAsyncCallback callback, object state)
		{
			return this.Operational.BeginInvoke(new IntAsyncDelegate(this.InitializeProcedure), callback, state);
		}
		/// <summary>
		/// Initialize 비동기 작업을 수행합니다.
		/// </summary>
		/// <returns>MethodCallerAsyncResult는 비동기 작업을 참조합니다.</returns>
		public MethodCallerAsyncResult BeginInitialize()
		{
			return this.BeginInitialize(null, null);
		}
		/// <summary>
		/// Initialize 비동기 작업을 완료합니다.
		/// </summary>
		/// <param name="ar">BeginBlock 메서드를 호출하여 반환된 MethodCallerAsyncResult입니다.</param>
		/// <returns>작업에 대한 결과를 반환한다. 0이면 성공 그렇지 않으면 실패를 나타냅니다</returns>
		public int EndInitialize(MethodCallerAsyncResult ar)
		{
			return (int)this.Operational.EndInvoke(ar);
		}
		/// <summary>
		/// Initialize 작업을 수행합니다.
		/// MethodCaller을 이용한 비동기 실행을 동기로 호출한다.
		/// </summary>
		/// <returns>작업에 대한 결과를 반환한다. 0이면 성공 그렇지 않으면 실패를 나타냅니다</returns>
		public int Initialize()
		{
			MethodCallerAsyncResult ar = this.BeginInitialize(null, null);
			return this.EndInitialize(ar);
		}

		/// <summary>
		/// Initialize 작업을 수행합니다.
		/// </summary>
		/// <returns>작업에 대한 결과를 반환한다. 0이면 성공 그렇지 않으면 실패를 나타냅니다</returns>
		public int InitializeSync()
		{
			this.Operational.Stop();
			return this.InitializeProcedure();
		}

		private int InitializeProcedure()
		{
			int ret = 0;
			if((ret = this.OnInitialize()) != 0) return ret;
			return ret;
		}

		protected abstract int OnInitialize();
		#endregion

		#endregion

		#region Element Members
		public new IPart Owner
		{
			get { return base.Owner as IPart; }
		}

		protected new PartInitializeAssistantConstructConfiguration ConstructConfiguration
		{
			get { return base.ConstructConfiguration as PartInitializeAssistantConstructConfiguration; }
		}

		protected override ElementConstructConfiguration OnGetDefaultConstructConfiguration()
		{
			return new PartInitializeAssistantConstructConfiguration();
		}

		protected override void OnSetConstructConfiguration(ElementConstructConfiguration configuration)
		{
			base.OnSetConstructConfiguration(configuration);

			if(this.ConstructConfiguration == null) return;
		}
		#endregion
	}
	#endregion

	#region PartInitializeAssistantConstructConfiguration
	[Serializable]
	public class PartInitializeAssistantConstructConfiguration : OperationalElementConstructConfiguration
	{
		#region Field
		#endregion

		#region Constructor
		public PartInitializeAssistantConstructConfiguration(ElementConstructMethod constructMethod)
			: base(ElementKind.Element, constructMethod)
		{
		}
		public PartInitializeAssistantConstructConfiguration() : this(ElementConstructMethod.Static) { }
		#endregion

		#region Property
		#endregion

		#region ConstructConfiguration Members
		protected override void SetDefaultValues()
		{
			base.SetDefaultValues();
		}
		#endregion
	}
	#endregion
}
