/*
 * Purpose
 *     Motions control library
 * 
 * Revision
 *     1. Created: 2009/04/30 
 * 
 */

using QMC.Common;
using QMC.Common.IO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text;


namespace QMC.Common.Motion.Ajin
{

    public static class AXM
    {
        #region Define

        #region Library
        public const string LibraryFileName = "Axl.dll";
        #endregion

        [Serializable]
        public enum MotorOutputMethod : uint
        {
            OneHighLowHigh,
            OneHighHighLow,
            OneLowLowHigh,
            OneLowHighLow,

            TwoCcwCwHigh,
            TwoCcwCwLow,
            TwoCwCcwHigh,
            TwoCwCcwLow,
        }

        [Serializable]
        public enum EncoderInputMethod : uint
        {
            ObverseUpDownMode,   // 정방향 Up/Down
            ObverseSqr1Mode,         // 정방향 1체배
            ObverseSqr2Mode,         // 정방향 2체배
            ObverseSqr4Mode,         // 정방향 4체배
            ReverseUpDownMode,   // 역방향 Up/Down
            ReverseSqr1Mode,         // 역방향 1체배
            ReverseSqr2Mode,         // 역방향 2체배
            ReverseSqr4Mode,         // 역방향 4체배
        }
        #endregion

        #region Dll Imports

        #region 보드 및 모듈 확인함수(Info) - Infomation

        // 해당 축의 보드번호, 모듈 위치, 모듈 아이디를 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmInfoGetAxis(int nAxisNo, ref int lpNodeNum, ref int npModulePos, ref uint upModuleID);
        // 모션 모듈이 존재하는지 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmInfoIsMotionModule(ref uint upStatus);
        // 해당 축이 유효한지 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmInfoIsInvalidAxisNo(int lAxisNo);
        // CAMC-QI 축 개수, 시스템에 장착된 유효한 모션 축수를 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmInfoGetAxisCount(ref int lpAxisCount);
        // 해당 노드/모듈의 첫번째 축번호를 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmInfoGetFirstAxisNo(int lNodeNum, int lModulePos, ref int lpAxisNo);

        #endregion

        #region 가상 축 함수

        // 초기 상태에서 AXM 모든 함수의 축번호 설정은 0 ~ (실제 시스템에 장착된 축수 - 1) 범위에서 유효하지만
        // 이 함수를 사용하여 실제 장착된 축번호 대신 임의의 축번호로 바꿀 수 있다.
        // 이 함수는 제어 시스템의 H/W 변경사항 발생시 기존 프로그램에 할당된 축번호를 그대로 유지하고 실제 제어 축의 
        // 물리적인 위치를 변경하여 사용을 위해 만들어진 함수이다.
        // 주의사항 : 여러 개의 실제 축번호에 대하여 같은 번호로 가상 축을 중복해서 맵핑할 경우 
        //            실제 축번호가 낮은 축만 가상 축번호로 제어 할 수 있으며, 
        //            나머지 같은 가상축 번호로 맵핑된 축은 제어가 불가능한 경우가 발생 할 수 있다.

        // 가상축을 설정한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmVirtualSetAxisNoMap(int nRealAxisNo, int nVirtualAxisNo);
        // 설정한 가상축 번호를 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmVirtualGetAxisNoMap(int nRealAxisNo, ref int npVirtualAxisNo);
        // 멀티 가상축을 설정한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmVirtualSetMultiAxisNoMap(int nSize, ref int npRealAxesNo, ref int npVirtualAxesNo);
        // 설정한 멀티 가상축 번호를 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmVirtualGetMultiAxisNoMap(int nSize, ref int npRealAxesNo, ref int npVirtualAxesNo);
        // 가상축 설정을 해지한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmVirtualResetAxisMap();

        #endregion

        #region 인터럽트 관련 함수
        // 콜백 함수 방식은 이벤트 발생 시점에 즉시 콜백 함수가 호출 됨으로 가장 빠르게 이벤트를 통지받을 수 있는 장점이 있으나
        // 콜백 함수가 완전히 종료 될 때까지 메인 프로세스가 정체되어 있게 된다.
        // 즉, 콜백 함수 내에 부하가 걸리는 작업이 있을 경우에는 사용에 주의를 요한다. 
        // 이벤트 방식은 쓰레드등을 이용하여 인터럽트 발생여부를 지속적으로 감시하고 있다가 인터럽트가 발생하면 
        // 처리해주는 방법으로, 쓰레드 등으로 인해 시스템 자원을 점유하고 있는 단점이 있지만
        // 가장 빠르게 인터럽트를 검출하고 처리해줄 수 있는 장점이 있다.
        // 일반적으로는 많이 쓰이지 않지만, 인터럽트의 빠른처리가 주요 관심사인 경우에 사용된다. 
        // 이벤트 방식은 이벤트의 발생 여부를 감시하는 특정 쓰레드를 사용하여 메인 프로세스와 별개로 동작되므로
        // MultiProcessor 시스템등에서 자원을 가장 효율적으로 사용할 수 있게 되어 특히 권장하는 방식이다.

        // 인터럽트 메시지를 받아오기 위하여 윈도우 메시지 또는 콜백 함수를 사용한다.
        // (메시지 핸들, 메시지 ID, 콜백함수, 인터럽트 이벤트)
        //    hWnd    : 윈도우 핸들, 윈도우 메세지를 받을때 사용. 사용하지 않으면 NULL을 입력.
        //    wMsg    : 윈도우 핸들의 메세지, 사용하지 않거나 디폴트값을 사용하려면 0을 입력.
        //    proc    : 인터럽트 발생시 호출될 함수의 포인터, 사용하지 않으면 NULL을 입력.
        //    pEvent  : 이벤트 방법사용시 이벤트 핸들
        [DllImport(LibraryFileName)]
        private static extern uint AxmInterruptSetAxis(int nAxisNo, uint hWnd, uint uMessage, CAXHS.AXT_INTERRUPT_PROC pProc, ref uint pEvent);

        // 설정 축의 인터럽트 사용 여부를 설정한다
        // 해당 축에 인터럽트 설정 / 확인
        // uUse : 사용 유무 => DISABLE(0), ENABLE(1)
        [DllImport(LibraryFileName)]
        private static extern uint AxmInterruptSetAxisEnable(int nAxisNo, uint uUse);
        // 설정 축의 인터럽트 사용 여부를 반환한다
        [DllImport(LibraryFileName)]
        private static extern uint AxmInterruptGetAxisEnable(int nAxisNo, ref uint upUse);

        //인터럽트를 이벤트 방식으로 사용할 경우 해당 인터럽트 정보 읽는다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmInterruptRead(ref int npAxisNo, ref uint upFlag);

        // 해당 축의 인터럽트 플래그 값을 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmInterruptReadAxisFlag(int nAxisNo, int nBank, ref uint upFlag);

        // 지정 축의 사용자가 설정한 인터럽트 발생 여부를 설정한다.
        // lBank         : 인터럽트 뱅크 번호 (0 - 1) 설정가능.
        // uInterruptNum : 인터럽트 번호 설정 비트번호로 설정 hex값 혹은 define된값을 설정
        // AXHS.h파일에 IP, QI INTERRUPT_BANK1, 2 DEF를 확인한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmInterruptSetUserEnable(int nAxisNo, int lBank, uint uInterruptNum);

        // 지정 축의 사용자가 설정한 인터럽트 발생 여부를 확인한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmInterruptGetUserEnable(int nAxisNo, int lBank, ref uint upInterruptNum);

        #endregion

        #region 모션 파라메타 설정
        // AxmMotLoadParaAll로 파일을 Load 시키지 않으면 초기 파라메타 설정시 기본 파라메타 설정. 
        // 현재 PC에 사용되는 모든축에 똑같이 적용된다. 기본파라메타는 아래와 같다. 
        // 00:AXIS_NO.             =0       01:PULSE_OUT_METHOD.    =4      02:ENC_INPUT_METHOD.    =3     03:INPOSITION.          =2
        // 04:ALARM.               =0       05:NEG_END_LIMIT.       =0      06:POS_END_LIMIT.       =0     07:MIN_VELOCITY.        =1
        // 08:MAX_VELOCITY.        =700000  09:HOME_SIGNAL.         =4      10:HOME_LEVEL.          =1     11:HOME_DIR.            =0
        // 12:ZPHASE_LEVEL.        =1       13:ZPHASE_USE.          =0      14:STOP_SIGNAL_MODE.    =0     15:STOP_SIGNAL_LEVEL.   =0
        // 16:HOME_FIRST_VELOCITY. =10000   17:HOME_SECOND_VELOCITY.=10000  18:HOME_THIRD_VELOCITY. =2000  19:HOME_LAST_VELOCITY.  =100
        // 20:HOME_FIRST_ACCEL.    =40000   21:HOME_SECOND_ACCEL.   =40000  22:HOME_END_CLEAR_TIME. =1000  23:HOME_END_OFFSET.     =0
        // 24:NEG_SOFT_LIMIT.      =0.000   25:POS_SOFT_LIMIT.      =0      26:MOVE_PULSE.          =1     27:MOVE_UNIT.           =1
        // 28:INIT_POSITION.       =1000    29:INIT_VELOCITY.       =200    30:INIT_ACCEL.          =400   31:INIT_DECEL.          =400
        // 32:INIT_ABSRELMODE.     =0       33:INIT_PROFILEMODE.    =4

        // 00=[AXIS_NO             ]: 축 (0축 부터 시작함)
        // 01=[PULSE_OUT_METHOD    ]: Pulse out method TwocwccwHigh = 6
        // 02=[ENC_INPUT_METHOD    ]: disable = 0   1체배 = 1  2체배 = 2  4체배 = 3, 결선 관련방향 교체시(-).1체배 = 11  2체배 = 12  4체배 = 13
        // 03=[INPOSITION          ], 04=[ALARM     ], 05,06 =[END_LIMIT   ]  : 0 = A접점 1= B접점 2 = 사용안함. 3 = 기존상태 유지
        // 07=[MIN_VELOCITY        ]: 시작 속도(START VELOCITY)
        // 08=[MAX_VELOCITY        ]: 드라이버가 지령을 받아들일수 있는 지령 속도. 보통 일반 Servo는 700k
        // Ex> screw : 20mm pitch drive: 10000 pulse 모터: 400w
        // 09=[HOME_SIGNAL         ]: 4 - Home in0 , 0 :PosEndLimit , 1 : NegEndLimit // _HOME_SIGNAL참조.
        // 10=[HOME_LEVEL          ]: 0 = A접점 1= B접점 2 = 사용안함. 3 = 기존상태 유지
        // 11=[HOME_DIR            ]: 홈 방향(HOME DIRECTION) 1:+방향, 0:-방향
        // 12=[ZPHASE_LEVEL        ]: 0 = A접점 1= B접점 2 = 사용안함. 3 = 기존상태 유지
        // 13=[ZPHASE_USE          ]: Z상사용여부. 0: 사용안함 , 1: +방향, 2: -방향 
        // 14=[STOP_SIGNAL_MODE    ]: ESTOP, SSTOP 사용시 모드 0:감속정지, 1:급정지 
        // 15=[STOP_SIGNAL_LEVEL   ]: ESTOP, SSTOP 사용 레벨.  0 = A접점 1= B접점 2 = 사용안함. 3 = 기존상태 유지 
        // 16=[HOME_FIRST_VELOCITY ]: 1차구동속도 
        // 17=[HOME_SECOND_VELOCITY]: 검출후속도 
        // 18=[HOME_THIRD_VELOCITY ]: 마지막 속도 
        // 19=[HOME_LAST_VELOCITY  ]: index검색및 정밀하게 검색하기위한 속도. 
        // 20=[HOME_FIRST_ACCEL    ]: 1차 가속도 , 21=[HOME_SECOND_ACCEL   ] : 2차 가속도 
        // 22=[HOME_END_CLEAR_TIME ]: 원점 검색 Enc 값 Set하기 위한 대기시간,  23=[HOME_END_OFFSET] : 원점검출후 Offset만큼 이동.
        // 24=[NEG_SOFT_LIMIT      ]: - SoftWare Limit 같게 설정하면 사용안함, 25=[POS_SOFT_LIMIT ]: + SoftWare Limit 같게 설정하면 사용안함.
        // 26=[MOVE_PULSE          ]: 드라이버의 1회전당 펄스량              , 27=[MOVE_UNIT  ]: 드라이버 1회전당 이동량 즉:스크류 Pitch
        // 28=[INIT_POSITION       ]: 에이젼트 사용시 초기위치  , 사용자가 임의로 사용가능
        // 29=[INIT_VELOCITY       ]: 에이젼트 사용시 초기속도  , 사용자가 임의로 사용가능
        // 30=[INIT_ACCEL          ]: 에이젼트 사용시 초기가속도, 사용자가 임의로 사용가능
        // 31=[INIT_DECEL          ]: 에이젼트 사용시 초기감속도, 사용자가 임의로 사용가능
        // 32=[INIT_ABSRELMODE     ]: 절대(0)/상대(1) 위치 설정
        // 33=[INIT_PROFILEMODE    ]: 프로파일모드(0 - 4) 까지 설정
        //                            '0': 대칭 Trapezode, '1': 비대칭 Trapezode, '2': 대칭 Quasi-S Curve, '3':대칭 S Curve, '4':비대칭 S Curve

        // AxmMotSaveParaAll로 저장 되어진 .mot파일을 불러온다. 해당 파일은 사용자가 Edit 하여 사용 가능하다.
        [DllImport(LibraryFileName)]
        public static extern uint AxmMotLoadParaAll(string szFilePath);
        // 모든축에 대한 모든 파라메타를 축별로 저장한다. .mot파일로 저장한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotSaveParaAll(string szFilePath);
        public static int LoadParameters(string filePath)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmMotLoadParaAll", AXM.AxmMotLoadParaAll(filePath))) != 0) return ret;
            return ret;
        }

        public static int SaveParameters(string filePath)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmMotSaveParaAll", AXM.AxmMotSaveParaAll(filePath))) != 0) return ret;
            return ret;
        }

        // 파라메타 28 - 31번까지 사용자가 프로그램내에서  이 함수를 이용해 설정 한다
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotSetParaLoad(int nAxisNo, double InitPos, double InitVel, double InitAccel, double InitDecel);
        // 파라메타 28 - 31번까지 사용자가 프로그램내에서  이 함수를 이용해 확인 한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotGetParaLoad(int nAxisNo, ref double InitPos, ref double InitVel, ref double InitAccel, ref double InitDecel);

        // 지정 축의 펄스 출력 방식을 설정한다.
        // uMethod  0 :OneHighLowHigh, 1 :OneHighHighLow, 2 :OneLowLowHigh, 3 :OneLowHighLow, 4 :TwoCcwCwHigh
        //          5 :TwoCcwCwLow, 6 :TwoCwCcwHigh, 7 :TwoCwCcwLow, 8 :TwoPhase, 9 :TwoPhaseReverse
        // OneHighLowHigh   = 0x0      // 1펄스 방식, PULSE(Active High), 정방향(DIR=Low)  / 역방향(DIR=High)
        // OneHighHighLow   = 0x1      // 1펄스 방식, PULSE(Active High), 정방향(DIR=High) / 역방향(DIR=Low)
        // OneLowLowHigh    = 0x2      // 1펄스 방식, PULSE(Active Low),  정방향(DIR=Low)  / 역방향(DIR=High)
        // OneLowHighLow    = 0x3      // 1펄스 방식, PULSE(Active Low),  정방향(DIR=High) / 역방향(DIR=Low)
        // TwoCcwCwHigh     = 0x4      // 2펄스 방식, PULSE(CCW:역방향),  DIR(CW:정방향),  Active High     
        // TwoCcwCwLow      = 0x5      // 2펄스 방식, PULSE(CCW:역방향),  DIR(CW:정방향),  Active Low     
        // TwoCwCcwHigh     = 0x6      // 2펄스 방식, PULSE(CW:정방향),   DIR(CCW:역방향), Active High
        // TwoCwCcwLow      = 0x7      // 2펄스 방식, PULSE(CW:정방향),   DIR(CCW:역방향), Active Low
        // TwoPhase         = 0x8      // 2상(90' 위상차),  PULSE lead DIR(CW: 정방향), PULSE lag DIR(CCW:역방향)
        // TwoPhaseReverse  = 0x9      // 2상(90' 위상차),  PULSE lead DIR(CCW: 정방향), PULSE lag DIR(CW:역방향)

        [DllImport(LibraryFileName)]
        private static extern uint AxmMotSetPulseOutMethod(int nAxisNo, uint uMethod);
        // 지정 축의 펄스 출력 방식 설정을 반환한다,
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotGetPulseOutMethod(int nAxisNo, ref uint upMethod);

        // 지정 축의 외부(Actual) 카운트의 증가 방향 설정을 포함하여 지정 축의 Encoder 입력 방식을 설정한다.
        // uMethod : 0 - 7 설정
        // ObverseUpDownMode    = 0x0      // 정방향 Up/Down
        // ObverseSqr1Mode      = 0x1      // 정방향 1체배
        // ObverseSqr2Mode      = 0x2      // 정방향 2체배
        // ObverseSqr4Mode      = 0x3      // 정방향 4체배
        // ReverseUpDownMode    = 0x4      // 역방향 Up/Down
        // ReverseSqr1Mode      = 0x5      // 역방향 1체배
        // ReverseSqr2Mode      = 0x6      // 역방향 2체배
        // ReverseSqr4Mode      = 0x7      // 역방향 4체배
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotSetEncInputMethod(int nAxisNo, uint uMethod);
        // 지정 축의 외부(Actual) 카운트의 증가 방향 설정을 포함하여 지정 축의 Encoder 입력 방식을 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotGetEncInputMethod(int nAxisNo, ref uint upMethod);

        // 설정 속도 단위가 RPM(Revolution Per Minute)으로 맞추고 싶다면.
        // ex>    rpm 계산:
        // 4500 rpm ?
        // unit/ pulse = 1 : 1이면      pulse/ sec 초당 펄스수가 되는데
        // 4500 rpm에 맞추고 싶다면     4500 / 60 초 : 75회전/ 1초
        // 모터가 1회전에 몇 펄스인지 알아야 된다. 이것은 Encoder에 Z상을 검색해보면 알수있다.
        // 1회전:1800 펄스라면 75 x 1800 = 135000 펄스가 필요하게 된다.
        // AxmMotSetMoveUnitPerPulse에 Unit = 1, Pulse = 1800 넣어 동작시킨다.
        // 주의할점 : rpm으로 제어하게 된다면 속도와 가속도 도 rpm단위로 바뀌게 된다.

        // 지정 축의 펄스 당 움직이는 거리를 설정한다.
        [DllImport(LibraryFileName)]
        public static extern uint AxmMotSetMoveUnitPerPulse(int nAxisNo, double dUnit, int nPulse);
        // 지정 축의 펄스 당 움직이는 거리를 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotGetMoveUnitPerPulse(int nAxisNo, ref double dpUnit, ref int npPulse);

        // 지정 축에 감속 시작 포인트 검출 방식을 설정한다.
        // uMethod : 0 -1 설정
        // AutoDetect = 0x0 : 자동 가감속.
        // RestPulse  = 0x1 : 수동 가감속."
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotSetDecelMode(int nAxisNo, uint uMethod);
        // 지정 축의 감속 시작 포인트 검출 방식을 반환한다    
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotGetDecelMode(int nAxisNo, ref uint upMethod);

        // 지정 축에 수동 감속 모드에서 잔량 펄스를 설정한다.
        // 사용방법: 만약 AxmMotSetRemainPulse를 500 펄스를 설정
        //           AxmMoveStartPos를 위치 10000을 보냈을경우에 9500펄스부터 
        //           남은 펄스 500은  AxmMotSetMinVel로 설정한 속도로 유지하면서 감속 된다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotSetRemainPulse(int nAxisNo, uint uData);
        // 지정 축의 수동 감속 모드에서 잔량 펄스를 반환한다.    
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotGetRemainPulse(int nAxisNo, ref uint upData);

        // 지정 축에 등속도 구동 함수에서의 최고 속도를 설정한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotSetMaxVel(int nAxisNo, double dVel);
        // 지정 축의 등속도 구동 함수에서의 최고 속도를 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotGetMaxVel(int nAxisNo, ref double dpVel);

        // 지정 축의 이동 거리 계산 모드를 설정한다.
        // uAbsRelMode  : POS_ABS_MODE '0' - 절대 좌표계
        //                POS_REL_MODE '1' - 상대 좌표계
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotSetAbsRelMode(int nAxisNo, uint uAbsRelMode);
        // 지정 축의 설정된 이동 거리 계산 모드를 반환한다
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotGetAbsRelMode(int nAxisNo, ref uint upAbsRelMode);

        // 지정 축의 구동 속도 프로파일 모드를 설정한다.
        // ProfileMode : SYM_TRAPEZOIDE_MODE    '0' - 대칭 Trapezode
        //               ASYM_TRAPEZOIDE_MODE   '1' - 비대칭 Trapezode
        //               QUASI_S_CURVE_MODE     '2' - 지원안함
        //               SYM_S_CURVE_MODE       '3' - 대칭 S Curve
        //               ASYM_S_CURVE_MODE      '4' - 비대칭 S Curve
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotSetProfileMode(int nAxisNo, uint uProfileMode);
        // 지정 축의 설정한 구동 속도 프로파일 모드를 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotGetProfileMode(int nAxisNo, ref uint upProfileMode);

        [Serializable]
        public enum AccelUnit : uint
        {
            UnitPerSec2 = 0,
            Second = 1
        }

        // 지정 축의 가속도 단위를 설정한다.
        // AccelUnit : UNIT_SEC2   '0' - 가감속 단위를 unit/sec2 사용
        //             SEC         '1' - 가감속 단위를 sec 사용
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotSetAccelUnit(int nAxisNo, uint uAccelUnit);
        // 지정 축의 설정된 가속도단위를 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotGetAccelUnit(int nAxisNo, ref uint upAccelUnit);

        // 지정 축에 초기 속도를 설정한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotSetMinVel(int nAxisNo, double dMinVelocity);
        // 지정 축의 초기 속도를 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotGetMinVel(int nAxisNo, ref double dpMinVelocity);

        // 지정 축의 가속 저크값을 설정한다.[%].
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotSetAccelJerk(int nAxisNo, double dAccelJerk);
        // 지정 축의 설정된 가속 저크값을 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotGetAccelJerk(int nAxisNo, ref double dpAccelJerk);

        // 지정 축의 감속 저크값을 설정한다.[%].
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotSetDecelJerk(int nAxisNo, double dDecelJerk);
        // 지정 축의 설정된 감속 저크값을 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotGetDecelJerk(int nAxisNo, ref double dpDecelJerk);

        #endregion

        #region 입출력 신호 관련 설정함수

        // 지정 축의 Z 상 Level을 설정한다.
        // uLevel : LOW(0), HIGH(1)
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalSetZphaseLevel(int nAxisNo, uint uLevel);
        // 지정 축의 Z 상 Level을 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalGetZphaseLevel(int nAxisNo, ref uint upLevel);

        // 지정 축의 Servo-On신호의 출력 레벨을 설정한다.
        // uLevel : LOW(0), HIGH(1)
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalSetServoOnLevel(int nAxisNo, uint uLevel);
        // 지정 축의 Servo-On신호의 출력 레벨 설정을 반환한다.    
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalGetServoOnLevel(int nAxisNo, ref uint upLevel);

        // 지정 축의 Servo-Alarm Reset 신호의 출력 레벨을 설정한다.
        // uLevel : LOW(0), HIGH(1)
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalSetServoAlarmResetLevel(int nAxisNo, uint uLevel);
        // 지정 축의 Servo-Alarm Reset 신호의 출력 레벨을 설정을 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalGetServoAlarmResetLevel(int nAxisNo, ref uint upLevel);

        // 지정 축의 Inpositon 신호 사용 여부 및 신호 입력 레벨을 설정한다
        // uLevel : LOW(0), HIGH(1), UNUSED(2), USED(3)    
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalSetInpos(int nAxisNo, uint uUse);
        // 지정 축의 Inpositon 신호 사용 여부 및 신호 입력 레벨을 반환한다.    
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalGetInpos(int nAxisNo, ref uint upUse);
        // 지정 축의 Inpositon 신호 입력 상태를 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalReadInpos(int nAxisNo, ref uint upStatus);

        // 지정 축의 알람 신호 입력 시 비상 정지의 사용 여부 및 신호 입력 레벨을 설정한다.
        // uLevel : LOW(0), HIGH(1), UNUSED(2), USED(3)
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalSetServoAlarm(int nAxisNo, uint uUse);
        // 지정 축의 알람 신호 입력 시 비상 정지의 사용 여부 및 신호 입력 레벨을 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalGetServoAlarm(int nAxisNo, ref uint upUse);
        // 지정 축의 알람 신호의 입력 레벨을 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalReadServoAlarm(int nAxisNo, ref uint upStatus);

        // 지정 축의 end limit sensor의 사용 유무 및 신호의 입력 레벨을 설정한다. 
        // end limit sensor 신호 입력 시 감속정지 또는 급정지에 대한 설정도 가능하다.
        // uStopMode: EMERGENCY_STOP(0), SLOWDOWN_STOP(1)
        // uPositiveLevel, uNegativeLevel : LOW(0), HIGH(1), UNUSED(2), USED(3)
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalSetLimit(int nAxisNo, uint uStopMode, uint uPositiveLevel, uint uNegativeLevel);
        // 지정 축의 end limit sensor의 사용 유무 및 신호의 입력 레벨, 신호 입력 시 정지모드를 반환한다
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalGetLimit(int nAxisNo, ref uint upStopMode, ref uint upPositiveLevel, ref uint upNegativeLevel);
        // 지정축의 end limit sensor의 입력 상태를 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalReadLimit(int nAxisNo, ref uint upPositiveStatus, ref uint upNegativeStatus);

        // 지정 축의 Software limit의 사용 유무, 사용할 카운트, 그리고 정지 방법을 설정한다
        // uUse       : DISABLE(0), ENABLE(1)
        // uStopMode  : EMERGENCY_STOP(0), SLOWDOWN_STOP(1)
        // uSelection : COMMAND(0), ACTUAL(1)
        // 주의사항: 원점검색시 위함수를 이용하여 소프트웨어 리밋을 미리 설정해서 구동시 원점검색시 원점검색을 도중에 멈추어졌을경우에도  Enable된다. 
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalSetSoftLimit(int nAxisNo, uint uUse, uint uStopMode, uint uSelection, double dPositivePos, double dNegativePos);
        // 지정 축의 Software limit의 사용 유무, 사용할 카운트, 그리고 정지 방법을 반환한다
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalGetSoftLimit(int nAxisNo, ref uint upUse, ref uint upStopMode, ref uint upSelection, ref double dpPositivePos, ref double dpNegativePos);

        // 비상 정지 신호의 정지 방법 (급정지/감속정지) 또는 사용 유무를 설정한다.
        // uStopMode  : EMERGENCY_STOP(0), SLOWDOWN_STOP(1)
        // uLevel : LOW(0), HIGH(1), UNUSED(2), USED(3)
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalSetStop(int nAxisNo, uint uStopMode, uint uLevel);
        // 비상 정지 신호의 정지 방법 (급정지/감속정지) 또는 사용 유무를 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalGetStop(int nAxisNo, ref uint upStopMode, ref uint upLevel);
        // 비상 정지 신호의 입력 상태를 반환한다.    
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalReadStop(int nAxisNo, ref uint upStatus);

        // 지정 축의 Servo-On 신호를 출력한다.
        // uOnOff : FALSE(0), TRUE(1) ( 범용 0출력에 해당됨)
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalServoOn(int nAxisNo, uint uUse);
        // 지정 축의 Servo-On 신호의 출력 상태를 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalIsServoOn(int nAxisNo, ref uint upUse);

        // 지정 축의 Servo-Alarm Reset 신호를 출력한다.
        // uOnOff : FALSE(0), TRUE(1) ( 범용 1출력에 해당됨)
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalServoAlarmReset(int nAxisNo, uint nOnOff);

        // 범용 출력값을 설정한다.
        // uValue : Hex Value 0x00
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalWriteOutput(int nAxisNo, uint uValue);
        // 범용 출력값을 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalReadOutput(int nAxisNo, ref uint upValue);

        // lBitNo : Bit Number(0 - 4)
        // uOnOff : FALSE(0), TRUE(1)
        // 범용 출력값을 비트별로 설정한다.
        [DllImport(LibraryFileName)]
        public static extern uint AxmSignalWriteOutputBit(int nAxisNo, int nBitNo, uint uOn);
        // 범용 출력값을 비트별로 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalReadOutputBit(int nAxisNo, int nBitNo, ref uint upOn);

        // 범용 입력값을 Hex값으로 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalReadInput(int nAxisNo, ref uint upValue);

        // lBitNo : Bit Number(0 - 4)
        // 범용 입력값을 비트별로 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalReadInputBit(int nAxisNo, int nBitNo, ref uint upOn);

        #endregion

        #region 모션 구동중 및 구동후에 상태 확인하는 함수

        // 지정 축의 펄스 출력 상태를 반환한다.
        // (구동상태)"
        [DllImport(LibraryFileName)]
        private static extern uint AxmStatusReadInMotion(int nAxisNo, ref uint upStatus);

        // 구동시작 이후 지정 축의 구동 펄스 카운터 값을 반환한다.
        // 주의사항: 구동중에만 카운터값을 표시하고 구동종료후에는 카운터값이 CLEAR된다.    
        //  (펄스 카운트 값)"
        [DllImport(LibraryFileName)]
        private static extern uint AxmStatusReadDrivePulseCount(int nAxisNo, ref int npPulse);

        // DriveStatus 레지스터를 확인
        [DllImport(LibraryFileName)]
        private static extern uint AxmStatusReadMotion(int nAxisNo, ref uint upStatus);



        // 지정 축의 모션 상태(Cmd, Act, Driver Status, Mechanical Signal, Universal Signal)를 한번에 확인 할 수 있다.
        // MOTION_INFO 구조체의 uMask 설정으로 모션 상태 정보를 지정한다.
        // uMask : 모션 상태 표시(6bit) - ex) uMask = 0x1F 설정 시 모든 상태를 표시함.
        // 사용자가 설정한 Level(In/Out)은 반영되지 않음.
        //    [0]        |    Command Position Read
        //    [1]        |    Actual Position Read
        //    [2]        |    Mechanical Signal Read
        //    [3]        |    Driver Status Read
        //    [4]        |    Universal Signal Input Read
        //               |    Universal Signal Output Read
        [DllImport(LibraryFileName)]
        private static extern uint AxmStatusReadMotionInfo(int nAxisNo, ref MOTION_INFO MI);


        // EndStatus 레지스터를 확인
        [DllImport(LibraryFileName)]
        private static extern uint AxmStatusReadStop(int nAxisNo, ref uint upStatus);

        // 지정 축의 Mechanical Signal Data(현재 기계적인 신호상태) 를 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmStatusReadMechanical(int nAxisNo, ref uint upStatus);

        // 지정 축의 현재 구동 속도를 읽어온다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmStatusReadVel(int nAxisNo, ref double dpVelocity);

        // Command Pos과 Actual Pos의 차를 확인
        [DllImport(LibraryFileName)]
        private static extern uint AxmStatusReadPosError(int nAxisNo, ref double dpError);

        // 최후 드라이브의 이동 거리를 확인
        [DllImport(LibraryFileName)]
        private static extern uint AxmStatusReadDriveDistance(int nAxisNo, ref double dpUnit);

        // 지정 축의 Actual 위치를 설정한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmStatusSetActPos(int nAxisNo, double dPos);
        // 지정 축의 Actual 위치를 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmStatusGetActPos(int nAxisNo, ref double dpPos);

        // 지정 축의 Command 위치를 설정한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmStatusSetCmdPos(int nAxisNo, double dPos);
        // 지정 축의 Command 위치를 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmStatusGetCmdPos(int nAxisNo, ref double dpPos);
        // 지정 축의 Torque 를 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmStatusReadTorque(int nAxisNo, ref double dpTorque);



        #endregion

        #region 홈관련 함수

        // 지정 축의 Home 센서 Level 을 설정한다.
        // uLevel : LOW(0), HIGH(1)
        [DllImport(LibraryFileName)]
        private static extern uint AxmHomeSetSignalLevel(int nAxisNo, uint uLevel);
        // 지정 축의 Home 센서 Level 을 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmHomeGetSignalLevel(int nAxisNo, ref uint upLevel);
        // 현재 홈 신호 입력상태를 확인한다. 홈신호는 사용자가 임의로 AxmHomeSetMethod 함수를 이용하여 설정할수있다.
        // upStatus : OFF(0), ON(1)
        [DllImport(LibraryFileName)]
        private static extern uint AxmHomeReadSignal(int nAxisNo, ref uint upStatus);

        // 해당 축의 원점검색을 수행하기 위해서는 반드시 원점 검색관련 파라메타들이 설정되어 있어야 됩니다. 
        // 만약 MotionPara설정 파일을 이용해 초기화가 정상적으로 수행됐다면 별도의 설정은 필요하지 않다. 
        // 원점검색 방법 설정에는 검색 진행방향, 원점으로 사용할 신호, 원점센서 Active Level, 엔코더 Z상 검출 여부 등을 설정 한다.
        // (자세한 내용은 AxmMotSaveParaAll 설명 부분 참조)
        // 홈레벨은 AxmSignalSetHomeLevel 사용한다.
        // HClrTim : HomeClear Time : 원점 검색 Encoder 값 Set하기 위한 대기시간 
        // HmDir(홈 방향): DIR_CCW (0) -방향 , DIR_CW(1) +방향
        // HOffset - 원점검출후 이동거리.
        // uZphas: 1차 원점검색 완료 후 엔코더 Z상 검출 유무 설정  0: 사용안함 , 1: +방향, 2: -방향 
        // HmSig : PosEndLimit(0) -> +Limit
        //         NegEndLimit(1) -> -Limit
        //         HomeSensor (4) -> 원점센서(범용 입력 0)
        [DllImport(LibraryFileName)]
        private static extern uint AxmHomeSetMethod(int nAxisNo, int nHmDir, uint uHomeSignal, uint uZphas, double dHomeClrTime, double dHomeOffset);
        // 설정되어있는 홈 관련 파라메타들을 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmHomeGetMethod(int nAxisNo, ref int nHmDir, ref uint uHomeSignal, ref uint uZphas, ref double dHomeClrTime, ref double dHomeOffset);

        // 원점을 빠르고 정밀하게 검색하기 위해 여러 단계의 스탭으로 검출한다. 이때 각 스탭에 사용 될 속도를 설정한다. 
        // 이 속도들의 설정값에 따라 원점검색 시간과, 원점검색 정밀도가 결정된다. 
        // 각 스탭별 속도들을 적절히 바꿔가면서 각 축의 원점검색 속도를 설정하면 된다. 
        // (자세한 내용은 AxmMotSaveParaAll 설명 부분 참조)
        // 원점검색시 사용될 속도를 설정하는 함수
        // [dVelFirst]- 1차구동속도   [dVelSecond]-검출후속도   [dVelThird]- 마지막 속도  [dvelLast]- index검색및 정밀하게 검색하기위해. 
        // [dAccFirst]- 1차구동가속도 [dAccSecond]-검출후가속도 
        [DllImport(LibraryFileName)]
        private static extern uint AxmHomeSetVel(int nAxisNo, double dVelFirst, double dVelSecond, double dVelThird, double dvelLast, double dAccFirst, double dAccSecond);
        // 설정되어있는 원점검색시 사용될 속도를 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmHomeGetVel(int nAxisNo, ref double dVelFirst, ref double dVelSecond, ref double dVelThird, ref double dvelLast, ref double dAccFirst, ref double dAccSecond);

        // 원점검색을 시작한다.
        // 원점검색 시작함수를 실행하면 라이브러리 내부에서 해당축의 원점검색을 수행 할 쓰레드가 자동 생성되어 원점검색을 순차적으로 수행한 후 자동 종료된다.
        // 주의사항 : 진행방향과 반대방향의 리미트 센서가 들어와도 진행방향의 센서가 ACTIVE되지않으면 동작한다.
        //            원점 검색이 시작되어 진행방향이 리밋트 센서가 들어오면 리밋트 센서가 감지되었다고 생각하고 다음단계로 진행된다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmHomeSetStart(int nAxisNo);

        // 원점검색 결과를 사용자가 임의로 설정한다.
        // 원점검색 함수를 이용해 성공적으로 원점검색이 수행되고나면 검색 결과가 HOME_SUCCESS로 설정됩니다.
        // 이 함수는 사용자가 원점검색을 수행하지않고 결과를 임의로 설정할 수 있다. 
        // uHomeResult 설정
        // HOME_SUCCESS              = 0x01      // 홈 완료
        // HOME_SEARCHING            = 0x02      // 홈검색중
        // HOME_ERR_GNT_RANGE        = 0x10      // 홈 검색 범위를 벗어났을경우
        // HOME_ERR_USER_BREAK       = 0x11      // 속도 유저가 임의로 정지명령을 내렸을경우
        // HOME_ERR_VELOCITY         = 0x12      // 속도 설정 잘못했을경우
        // HOME_ERR_AMP_FAULT        = 0x13      // 서보팩 알람 발생 에러
        // HOME_ERR_NEG_LIMIT        = 0x14      // (-)방향 구동중 (+)리미트 센서 감지 에러
        // HOME_ERR_POS_LIMIT        = 0x15      // (+)방향 구동중 (-)리미트 센서 감지 에러
        // HOME_ERR_NOT_DETECT       = 0x16      // 지정한 신호 검출하지 못 할 경우 에러
        // HOME_ERR_UNKNOWN          = 0xFF    
        [DllImport(LibraryFileName)]
        private static extern uint AxmHomeSetResult(int nAxisNo, uint uHomeResult);
        // 원점검색 결과를 반환한다.
        // 원점검색 함수의 검색 결과를 확인한다. 원점검색이 시작되면 HOME_SEARCHING으로 설정되며 원점검색에 실패하면 실패원인이 설정된다. 실패 원인을 제거한 후 다시 원점검색을 진행하면 된다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmHomeGetResult(int nAxisNo, ref uint upHomeResult);
        // 원점검색 진행률을 반환한다.
        // 원점검색 시작되면 진행율을 확인할 수 있다. 원점검색이 완료되면 성공여부와 관계없이 100을 반환하게 된다. 원점검색 성공여부는 GetHome Result함수를 이용해 확인할 수 있다.
        // upHomeMainStepNumber : Main Step 진행율이다. 
        // 겐트리 FALSE일 경우upHomeMainStepNumber : 0 일때면 선택한 축만 진행사항이고 홈 진행율은 upHomeStepNumber 표시한다.
        // 겐트리 TRUE일 경우 upHomeMainStepNumber : 0 일때면 마스터 홈을 진행사항이고 마스터 홈 진행율은 upHomeStepNumber 표시한다.
        // 겐트리 TRUE일 경우 upHomeMainStepNumber : 10 일때면 슬레이브 홈을 진행사항이고 마스터 홈 진행율은 upHomeStepNumber 표시한다.
        // upHomeStepNumber     : 선택한 축에대한 진행율을 표시한다. 
        // 겐트리 FALSE일 경우  : 선택한 축만 진행율을 표시한다.
        // 겐트리 TRUE일 경우 마스터축, 슬레이브축 순서로 진행율을 표시된다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmHomeGetRate(int nAxisNo, ref uint upHomeMainStepNumber, ref uint upHomeStepNumber);

        #endregion

        #region 위치 구동함수

        // 주의사항: 위치를 설정할경우 반드시 UNIT/PULSE의 맞추어서 설정한다.
        //           위치를 UNIT/PULSE 보다 작게할 경우 최소단위가 UNIT/PULSE로 맞추어지기때문에 그위치까지 구동이 될수없다.
        // 설정 속도 단위가 RPM(Revolution Per Minute)으로 맞추고 싶다면.
        // ex>    rpm 계산:
        // 4500 rpm ?
        // unit/ pulse = 1 : 1이면      pulse/ sec 초당 펄스수가 되는데
        // 4500 rpm에 맞추고 싶다면     4500 / 60 초 : 75회전/ 1초
        // 모터가 1회전에 몇 펄스인지 알아야 된다. 이것은 Encoder에 Z상을 검색해보면 알수있다.
        // 1회전:1800 펄스라면 75 x 1800 = 135000 펄스가 필요하게 된다.
        // AxmMotSetMoveUnitPerPulse에 Unit = 1, Pulse = 1800 넣어 동작시킨다. 

        // 설정한 거리만큼 또는 위치까지 이동한다.
        // 지정 축의 절대 좌표/ 상대좌표 로 설정된 위치까지 설정된 속도와 가속율로 구동을 한다.
        // 속도 프로파일은 AxmMotSetProfileMode 함수에서 설정한다.
        // 펄스가 출력되는 시점에서 함수를 벗어난다.
        // Vel값이 양수이면 CW, 음수이면 CCW 방향으로 구동.
        // AxmMotSetAccelUnit(lAxisNo, 1) 일경우 dAccel -> dAccelTime , dDecel -> dDecelTime 으로 바뀐다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMoveStartPos(int nAxisNo, double dPos, double dVel, double dAccel, double dDecel);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nAxisNo"></param>
        /// <param name="dPos"></param>
        /// <param name="dVel"></param>
        /// <param name="dAccel"></param>
        /// <param name="dDecel"></param>
        /// <returns></returns>
        [DllImport(LibraryFileName)]
        private static extern uint AxmMoveStartPosWithList(int nAxisNo, double dPos, ref double dVel, ref double dAccel, ref double dDecel, int count);

        // 설정한 거리만큼 또는 위치까지 이동한다.
        // 지정 축의 절대 좌표/상대좌표로 설정된 위치까지 설정된 속도와 가속율로 구동을 한다.
        // 속도 프로파일은 AxmMotSetProfileMode 함수에서 설정한다. 
        // 펄스 출력이 종료되는 시점에서 함수를 벗어난다
        // Vel값이 양수이면 CW, 음수이면 CCW 방향으로 구동.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMovePos(int nAxisNo, double dPos, double dVel, double dAccel, double dDecel);

        // 설정한 속도로 구동한다.
        // 지정 축에 대하여 설정된 속도와 가속율로 지속적으로 속도 모드 구동을 한다. 
        // 펄스 출력이 시작되는 시점에서 함수를 벗어난다.
        // Vel값이 양수이면 CW, 음수이면 CCW 방향으로 구동.
        [DllImport(LibraryFileName)]
        public static extern uint AxmMoveVel(int nAxisNo, double dVel, double dAccel, double dDecel);

        // 지정된 다축에 대하여 설정된 속도와 가속율로 지속적으로 속도 모드 구동을 한다.
        // 펄스 출력이 시작되는 시점에서 함수를 벗어난다.
        // Vel값이 양수이면 CW, 음수이면 CCW 방향으로 구동.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMoveStartMultiVel(int lArraySize, ref int lpAxesNo, ref double dVel, ref double dAccel, ref double dDecel);

        // 특정 Input 신호의 Edge를 검출하여 즉정지 또는 감속정지하는 함수.
        // lDetect Signal : edge 검출할 입력 신호 선택.
        // lDetectSignal  : PosEndLimit(0), NegEndLimit(1), HomeSensor(4), EncodZPhase(5), UniInput02(6), UniInput03(7)
        // Signal Edge    : 선택한 입력 신호의 edge 방향 선택 (rising or falling edge).
        //                    SIGNAL_DOWN_EDGE(0), SIGNAL_UP_EDGE(1)
        // 구동방향       : Vel값이 양수이면 CW, 음수이면 CCW.
        // SignalMethod   : 급정지 EMERGENCY_STOP(0), 감속정지 SLOWDOWN_STOP(1)
        // 주의사항 : SignalMethod를 EMERGENCY_STOP(0)로 사용할경우 가감속이 무시되며 지정된 속도로 가속 급정지하게된다.
        //            PCI-Nx04를 사용할 경우 lDetectSignal이 PosEndLimit , NegEndLimit(0,1) 을 찾을경우 신호의레벨 Active 상태를 검출하게된다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMoveSignalSearch(int nAxisNo, double dVel, double dAccel, int nDetectSignal, int nSignalEdge, int nSignalMethod);

        // 지정 축에서 설정된 신호를 검출하고 그 위치를 저장하기 위해 이동하는 함수이다.
        // 원하는 신호를 골라 찾아 움직이는 함수 찾을 경우 그 위치를 저장시켜놓고 AxmGetCapturePos사용하여 그값을 읽는다.
        // Signal Edge   : 선택한 입력 신호의 edge 방향 선택 (rising or falling edge).
        //                 SIGNAL_DOWN_EDGE(0), SIGNAL_UP_EDGE(1)
        // 구동방향      : Vel값이 양수이면 CW, 음수이면 CCW.
        // SignalMethod  : 급정지 EMERGENCY_STOP(0), 감속정지 SLOWDOWN_STOP(1)
        // lDetect Signal: edge 검출할 입력 신호 선택.SIGNAL_DOWN_EDGE(0), SIGNAL_UP_EDGE(1)
        // lDetectSignal : PosEndLimit(0), NegEndLimit(1), HomeSensor(4), EncodZPhase(5), UniInput02(6), UniInput03(7)
        // lTarget       : COMMAND(0), ACTUAL(1)
        // 주의사항: SignalMethod를 EMERGENCY_STOP(0)로 사용할경우 가감속이 무시되며 지정된 속도로 가속 급정지하게된다.
        //           lDetectSignal이 PosEndLimit , NegEndLimit(0,1) 을 찾을경우 신호의레벨 Active 상태를 검출하게된다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMoveSignalCapture(int nAxisNo, double dVel, double dAccel, int nDetectSignal, int nSignalEdge, int nTarget, int nSignalMethod);

        // 'AxmMoveSignalCapture' 함수에서 저장된 위치값을 확인하는 함수이다.
        // 주의사항: 함수 실행 결과가 "AXT_RT_SUCCESS"일때 저장된 위치가 유효하며, 이 함수를 한번 실행하면 저장 위치값이 초기화된다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMoveGetCapturePos(int nAxisNo, ref double dpCapPos);

        // "설정한 거리만큼 또는 위치까지 이동하는 함수.
        // 함수를 실행하면 해당 Motion 동작을 시작한 후 Motion 이 완료될때까지 기다리지 않고 바로 함수를 빠져나간다."
        [DllImport(LibraryFileName)]
        private static extern uint AxmMoveStartMultiPos(int nArraySize, ref int nAxisNo, ref double dPos, ref double dVel, ref double dAccel, ref double dDecel);

        // 다축을 설정한 거리만큼 또는 위치까지 이동한다.
        // 지정 축들의 절대 좌표로 설정된 위치까지 설정된 속도와 가속율로 구동을 한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMoveMultiPos(int nArraySize, ref int nAxisNo, ref double dPos, ref double dVel, ref double dAccel, ref double dDecel);

        // 지정 축을 설정한 감속도로 감속 정지 한다.
        // dDecel : 정지 시 감속율값
        [DllImport(LibraryFileName)]
        private static extern uint AxmMoveStop(int nAxisNo, double dDecel);
        // 지정 축을 급 정지 한다.
        [DllImport(LibraryFileName)]
        public static extern uint AxmMoveEStop(int nAxisNo);
        // 지정 축을 감속 정지한다.
        [DllImport(LibraryFileName)]
        public static extern uint AxmMoveSStop(int nAxisNo);

        #endregion

        #region 오버라이드 함수

        // 위치 오버라이드 한다.
        // 지정 축의 구동이 종료되기 전 지정된 출력 펄스 수를 조정한다.
        // PCI-Nx04 사용시주의사항: 오버라이드할 위치를 넣을때는 구동 시점의 위치를 기준으로한 Relative 형태의 위치값으로 넣어준다.
        //                          구동시작후 같은방향의 경우 오버라이드를 계속할수있지만 반대방향으로 오버라이드할경우에는 오버라이드를 계속할수없다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmOverridePos(int nAxisNo, double dOverridePos);

        // 지정 축의 속도오버라이드 하기전에 오버라이드할 최고속도를 설정한다.
        // 주의점 : 속도오버라이드를 5번한다면 그중에 최고 속도를 설정해야된다. 
        [DllImport(LibraryFileName)]
        private static extern uint AxmOverrideSetMaxVel(int nAxisNo, double dOverrideMaxVel);

        // 속도 오버라이드 한다.
        // 지정 축의 구동 중에 속도를 가변 설정한다. (반드시 모션 중에 가변 설정한다.)
        // 주의점: AxmOverrideVel 함수를 사용하기전에. AxmOverrideMaxVel 최고로 설정할수있는 속도를 설정해놓는다.
        // EX> 속도오버라이드를 두번한다면 
        // 1. 두개중에 높은 속도를 AxmOverrideMaxVel 설정 최고 속도값 설정.
        // 2. AxmMoveStartPos 실행 지정 축의 구동 중(Move함수 모두 포함)에 속도를 첫번째 속도로 AxmOverrideVel 가변 설정한다.
        // 3. 지정 축의 구동 중(Move함수 모두 포함)에 속도를 두번째 속도로 AxmOverrideVel 가변 설정한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmOverrideVel(int nAxisNo, double dOverrideVelocity);

        // 가속도, 속도, 감속도를  오버라이드 한다.
        // 지정 축의 구동 중에 가속도, 속도, 감속도를 가변 설정한다. (반드시 모션 중에 가변 설정한다.)
        // 주의점: AxmOverrideAccelVelDecel 함수를 사용하기전에. AxmOverrideMaxVel 최고로 설정할수있는 속도를 설정해놓는다.
        // EX> 속도오버라이드를 두번한다면 
        // 1. 두개중에 높은 속도를 AxmOverrideMaxVel 설정 최고 속도값 설정.
        // 2. AxmMoveStartPos 실행 지정 축의 구동 중(Move함수 모두 포함)에 가속도, 속도, 감속도를 첫번째 속도로 AxmOverrideAccelVelDecel 가변 설정한다.
        // 3. 지정 축의 구동 중(Move함수 모두 포함)에 가속도, 속도, 감속도를 두번째 속도로 AxmOverrideAccelVelDecel 가변 설정한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmOverrideAccelVelDecel(int nAxisNo, double dOverrideVelocity, double dMaxAccel, double dMaxDecel);

        // 어느 시점에서 속도 오버라이드 한다.
        // 어느 위치 지점과 오버라이드할 속도를 입력시켜 그위치에서 속도오버라이드 되는 함수
        // lTarget : COMMAND(0), ACTUAL(1)
        // 주의점: AxmOverrideVelAtPos 함수를 사용하기전에. AxmOverrideMaxVel 최고로 설정할수있는 속도를 설정해놓는다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmOverrideVelAtPos(int nAxisNo, double dPos, double dVel, double dAccel, double dDecel, double dOverridePos, double dOverrideVelocity, int nTarget);

        #endregion

        #region 마스터, 슬레이브  기어비로 구동 함수

        // Electric Gear 모드에서 Master 축과 Slave 축과의 기어비를 설정한다.
        // dSlaveRatio : 마스터축에 대한 슬레이브의 기어비( 0 : 0% , 0.5 : 50%, 1 : 100%)
        [DllImport(LibraryFileName)]
        private static extern uint AxmLinkSetMode(int nMasterAxisNo, int nSlaveAxisNo, double dSlaveRatio);
        // Electric Gear 모드에서 설정된 Master 축과 Slave 축과의 기어비를 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmLinkGetMode(int nMasterAxisNo, ref uint nSlaveAxisNo, ref double dpGearRatio);
        // Master 축과 Slave축간의 전자기어비를 설정 해제 한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmLinkResetMode(int nMasterAxisNo);

        #endregion

        #region 겐트리 관련 함수
        [Serializable]
        public enum GantryHomingMethods
        {
            OnlyMaster = 0,
            MasterSlaveWithOffset = 1,
            MasterSlaveWithoutOffset = 2,
        }

        // 모션모듈은 두 축이 기구적으로 Link되어있는 겐트리 구동시스템 제어를 지원한다. 
        // 이 함수를 이용해 Master축을 겐트리 제어로 설정하면 해당 Slave축은 Master축과 동기되어 구동됩니다. 
        // 만약 겐트리 설정 이후 Slave축에 구동명령이나 정지 명령등을 내려도 모두 무시됩니다.
        // uSlHomeUse     : 슬레이축 홈사용 우뮤 (0 - 2)
        //             (0 : 슬레이브축 홈을 사용안하고 마스터축을 홈을 찾는다.)
        //             (1 : 마스터축 , 슬레이브축 홈을 찾는다. 슬레이브 dSlOffset 값 적용해서 보정함.)
        //             (2 : 마스터축 , 슬레이브축 홈을 찾는다. 슬레이브 dSlOffset 값 적용해서 보정안함.)
        // dSlOffset      : 슬레이브축 옵셋값
        // dSlOffsetRange : 슬레이브축 옵셋값 레인지 설정
        // PCI-Nx04 사용시주의사항: 갠트리 ENABLE시 슬레이브축은 모션중 AxmStatusReadMotion 함수로 확인하면 True(Motion 구동 중)로 확인되야 정상동작이다. 
        //                   슬레이브축에 AxmStatusReadMotion로 확인했을때 InMotion 이 False이면 Gantry Enable이 안된것이므로 알람 혹은 리밋트 센서 등을 확인한다.
        [DllImport(LibraryFileName)]
        public static extern uint AxmGantrySetEnable(int nMasterAxisNo, int nSlaveAxisNo, uint uSlHomeUse, double dSlOffset, double dSlOffsetRange);

        // Slave축의 Offset값을 알아내는방법.
        // A. 마스터, 슬레이브를 두개다 서보온을 시킨다.         
        // B. AxmGantrySetEnable함수에서 uSlHomeUse = 2로 설정후 AxmHomeSetStart함수를 이용해서 홈을 찾는다. 
        // C. 홈을 찾고 나면 마스터축의 Command값을 읽어보면 마스터축과 슬레이브축의 틀어진 Offset값을 볼수있다.
        // D. Offset값을 읽어서 AxmGantrySetEnable함수의 dSlOffset인자에 넣어준다. 
        // E. dSlOffset값을 넣어줄때 마스터축에 대한 슬레이브 축 값이기때문에 부호를 반대로 -dSlOffset 넣어준다.
        // F. dSIOffsetRange 는 Slave Offset의 Range 범위를 말하는데 Range의 한계를 지정하여 한계를 벗어나면 에러를 발생시킬때 사용한다.        
        // G. AxmGantrySetEnable함수에 Offset값을 넣어줬으면  AxmGantrySetEnable함수에서 uSlHomeUse = 1로 설정후 AxmHomeSetStart함수를 이용해서 홈을 찾는다.         

        // 겐트리 구동에 있어 사용자가 설정한 파라메타를 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmGantryGetEnable(int nMasterAxisNo, ref uint upSlHomeUse, ref double dpSlOffset, ref double dSlORange, ref uint uGatryOn);

        // 모션 모듈은 두 축이 기구적으로 Link되어있는 겐트리 구동시스템 제어를 해제한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmGantrySetDisable(int nMasterAxisNo, int nSlaveAxisNo);

        #endregion

        #region 일반 보간함수

        // 주의사항1: AxmContiSetAxisMap함수를 이용하여 축맵핑후에 낮은순서축부터 맵핑을 하면서 사용해야된다.
        //           원호보간의 경우에는 반드시 낮은순서축부터 축배열에 넣어야 동작 가능하다.

        // 주의사항2: 위치를 설정할경우 반드시 마스터축과 슬레이브 축의 UNIT/PULSE의 맞추어서 설정한다.
        //           위치를 UNIT/PULSE 보다 작게 설정할 경우 최소단위가 UNIT/PULSE로 맞추어지기때문에 그위치까지 구동이 될수없다.

        // 주의사항3: 원호 보간을 할경우 반드시 한칩내에서 구동이 될수있으므로 

        // 주의사항4: 보간 구동 시작/중에 비정상 정지 조건(+- Limit신호, 서보 알람, 비상정지 등)이 발생하면 
        //            구동 방향에 상관없이 구동을 시작하지 않거나 정지 된다.

        // 직선 보간 한다.
        // 시작점과 종료점을 지정하여 다축 직선 보간 구동하는 함수이다. 구동 시작 후 함수를 벗어난다.
        // AxmContiBeginNode, AxmContiEndNode와 같이사용시 지정된 좌표계에 시작점과 종료점을 지정하여 직선 보간 구동하는 Queue에 저장함수가된다. 
        // 직선 프로파일 연속 보간 구동을 위해 내부 Queue에 저장하여 AxmContiStart함수를 사용해서 시작한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmLineMove(int lCoord, ref double dPos, double dVel, double dAccel, double dDecel);

        // 2축 원호보간 한다.
        // 시작점, 종료점과 중심점을 지정하여 원호 보간 구동하는 함수이다. 구동 시작 후 함수를 벗어난다.
        // AxmContiBeginNode, AxmContiEndNode, 와 같이사용시 지정된 좌표계에 시작점, 종료점과 중심점을 지정하여 구동하는 원호 보간 Queue에 저장함수가된다.
        // 프로파일 원호 연속 보간 구동을 위해 내부 Queue에 저장하여 AxmContiStart함수를 사용해서 시작한다.
        // dCenterPos = 중심점 X,Y  , dEndPos = 종료점 X,Y .
        // uCWDir   DIR_CCW(0): 반시계방향, DIR_CW(1) 시계방향
        [DllImport(LibraryFileName)]
        private static extern uint AxmCircleCenterMove(int lCoord, ref int lAxisNo, ref double dCenterPos, ref double dEndPos, double dVel, double dAccel, double dDecel, uint uCWDir);

        // 중간점, 종료점을 지정하여 원호 보간 구동하는 함수이다. 구동 시작 후 함수를 벗어난다.
        // AxmContiBeginNode, AxmContiEndNode와 같이사용시 지정된 좌표계에 중간점, 종료점을 지정하여 구동하는 원호 보간 Queue에 저장함수가된다.
        // 프로파일 원호 연속 보간 구동을 위해 내부 Queue에 저장하여 AxmContiStart함수를 사용해서 시작한다.
        // dMidPos = 중간점 X,Y  , dEndPos = 종료점 X,Y 
        // uCWDir   DIR_CCW(0): 반시계방향, DIR_CW(1) 시계방향
        [DllImport(LibraryFileName)]
        private static extern uint AxmCirclePointMove(int lCoord, ref int lAxisNo, ref double dMidPos, ref double dEndPos, double dVel, double dAccel, double dDecel);

        // 시작점, 종료점과 반지름을 지정하여 원호 보간 구동하는 함수이다. 구동 시작 후 함수를 벗어난다.
        // AxmContiBeginNode, AxmContiEndNode와 같이사용시 지정된 좌표계에 시작점, 종료점과 반지름을 지정하여 원호 보간 구동하는 Queue에 저장함수가된다.
        // 프로파일 원호 연속 보간 구동을 위해 내부 Queue에 저장하여 AxmContiStart함수를 사용해서 시작한다.
        // lAxisNo = 두축 배열 , dRadius = 반지름, dEndPos = 종료점 X,Y 배열 , uShortDistance = 작은원(0), 큰원(1)
        // uCWDir   DIR_CCW(0): 반시계방향, DIR_CW(1) 시계방향
        [DllImport(LibraryFileName)]
        private static extern uint AxmCircleRadiusMove(int lCoord, ref int lAxisNo, double dRadius, ref double dEndPos, double dVel, double dAccel, double dDecel, uint uCWDir, uint uShortDistance);

        // 시작점, 회전각도와 반지름을 지정하여 원호 보간 구동하는 함수이다. 구동 시작 후 함수를 벗어난다.
        // AxmContiBeginNode, AxmContiEndNode와 같이사용시 지정된 좌표계에 시작점, 회전각도와 반지름을 지정하여 원호 보간 구동하는 Queue에 저장함수가된다.
        // 프로파일 원호 연속 보간 구동을 위해 내부 Queue에 저장하여 AxmContiStart함수를 사용해서 시작한다.
        // dCenterPos = 중심점 X,Y  , dAngle = 각도.
        // uCWDir   DIR_CCW(0): 반시계방향, DIR_CW(1) 시계방향
        [DllImport(LibraryFileName)]
        private static extern uint AxmCircleAngleMove(int lCoord, ref int lAxisNo, ref double dCenterPos, double dAngle, double dVel, double dAccel, double dDecel, uint uCWDir);

        #endregion

        #region 연속 보간 함수

        //지정된 좌표계에 연속보간 축 맵핑을 설정한다.
        //(축맵핑 번호는 0 부터 시작))
        // 주의점: 축맵핑할때는 반드시 실제 축번호가 작은 숫자부터 큰숫자를 넣는다.
        //         가상축 맵핑 함수를 사용하였을 때 가상축번호를 실제 축번호가 작은 값 부터 lpAxesNo의 낮은 인텍스에 입력하여야 한다.
        //         가상축 맵핑 함수를 사용하였을 때 가상축번호에 해당하는 실제 축번호가 다른 값이라야 한다.
        //         SMC-2V03의 경우 lSize는 2로 입력하여야 한다.
        //         같은 축을 다른 Coordinate에 중복 맵핑하지 말아야 한다.

        [DllImport(LibraryFileName)]
        private static extern uint AxmContiSetAxisMap(int lCoord, uint lSize, ref int lpRealAxesNo);
        //지정된 좌표계에 연속보간 축 맵핑을 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmContiGetAxisMap(int lCoord, ref uint lSize, ref int lpRealAxesNo);

        // 지정된 좌표계에 연속보간 축 절대/상대 모드를 설정한다.
        // (주의점 : 반드시 축맵핑 하고 사용가능)
        // 지정 축의 이동 거리 계산 모드를 설정한다.
        //uAbsRelMode : POS_ABS_MODE '0' - 절대 좌표계
        //              POS_REL_MODE '1' - 상대 좌표계

        [DllImport(LibraryFileName)]
        private static extern uint AxmContiSetAbsRelMode(int lCoord, uint uAbsRelMode);
        // 지정된 좌표계에 연속보간 축 절대/상대 모드를 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmContiGetAbsRelMode(int lCoord, ref uint upAbsRelMode);

        // 지정된 좌표계에 보간 구동을 위한 내부 Queue가 비어 있는지 확인하는 함수이다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmContiReadFree(int lCoord, ref uint upQueueFree);
        // 지정된 좌표계에 보간 구동을 위한 내부 Queue에 저장되어 있는 보간 구동 개수를 확인하는 함수이다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmContiReadIndex(int lCoord, ref int npQueueIndex);
        // 지정된 좌표계에 연속 보간 구동을 위해 저장된 내부 Queue를 모두 삭제하는 함수이다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmContiWriteClear(int lCoord);

        // 지정된 좌표계에 연속보간에서 수행할 작업들의 등록을 시작한다. 이함수를 호출한후,
        // AxmContiEndNode함수가 호출되기 전까지 수행되는 모든 모션작업은 실제 모션을 수행하는 것이 아니라 연속보간 모션으로 등록 되는 것이며,
        // AxmContiStart 함수가 호출될 때 비로소 등록된모션이 실제로 수행된다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmContiBeginNode(int lCoord);
        // 지정된 좌표계에서 연속보간을 수행할 작업들의 등록을 종료한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmContiEndNode(int lCoord);

        // 연속 보간 시작 한다.
        // SMC-2V03 module :  dwProfileset, lAngle 값을 0으로 입력함. 
        // PCI-Nx04 : dwProfileset(CONTI_NODE_VELOCITY(0) : 연속 보간 사용, CONTI_NODE_MANUAL(1) : 프로파일 보간 사용, CONTI_NODE_AUTO(2) : 자동 프로파일 보간, 3 : 속도보상 모드 사용) 
        [DllImport(LibraryFileName)]
        private static extern uint AxmContiStart(int lCoord, uint dwProfileset, int lAngle);
        // 지정된 좌표계에 연속 보간 구동 중인지 확인하는 함수이다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmContiIsMotion(int lCoord, ref uint upInMotion);
        // 지정된 좌표계에 연속 보간 구동 중 현재 구동중인 연속 보간 인덱스 번호를 확인하는 함수이다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmContiGetNodeNum(int lCoord, ref int npNodeNum);
        // 지정된 좌표계에 설정한 연속 보간 구동 총 인덱스 갯수를 확인하는 함수이다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmContiGetTotalNodeNum(int lCoord, ref int npNodeNum);

        #endregion

        #region 트리거 함수

        // 주의사항: 트리거 위치를 설정할경우 반드시 UNIT/PULSE의 맞추어서 설정한다.
        //           위치를 UNIT/PULSE 보다 작게할 경우 최소단위가 UNIT/PULSE로 맞추어지기때문에 그위치에 출력할수없다.

        // 지정 축에 트리거 기능의 사용 여부, 출력 레벨, 위치 비교기, 트리거 신호 지속 시간 및 트리거 출력 모드를 설정한다.
        // 트리거 기능 사용을 위해서는 먼저  AxmTriggerSetTimeLevel 를 사용하여 관련 기능 설정을 먼저 하여야 한다.
        // dTrigTime        : 트리거 출력 시간 
        //                    1usec - 최대 50msec ( 1 - 50000 까지 설정)
        // upTriggerLevel   : 트리거 출력 레벨 유무   => LOW(0), HIGH(1)
        // uSelect          : 사용할 기준 위치        => COMMAND(0), ACTUAL(1)
        // uInterrupt       : 인터럽트 설정           => DISABLE(0), ENABLE(1)

        // 지정 축에 트리거 신호 지속 시간 및 트리거 출력 레벨, 트리거 출력방법을 설정한다.
        [DllImport(LibraryFileName)]
        public static extern uint AxmTriggerSetTimeLevel(int lAxisNo, double dTrigTime, uint uTriggerLevel, uint uSelect, uint uInterrupt);
        // 지정 축에 트리거 신호 지속 시간 및 트리거 출력 레벨, 트리거 출력방법을 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmTriggerGetTimeLevel(int lAxisNo, ref double dTrigTime, ref uint uTriggerLevel, ref uint uSelect, ref uint uInterrupt);

        // 지정 축의 트리거 출력 기능을 설정한다.
        // uMethod : PERIOD_MODE      0x0 : 현재 위치를 기준으로 dPos를 위치 주기로 사용한 주기 트리거 방식
        //           ABS_POS_MODE     0x1 : 트리거 절대 위치에서 트리거 발생, 절대 위치 방식

        // dPos    : 주기 선택시 : 위치마다위치마다 출력하기때문에 그 위치
        //           절대 선택시 : 출력할 그 위치, 이 위치와같으면 무조건 출력이 나간다. 
        // 주의사항: N404, N804의 경우에는 AxmTriggerSetAbsPeriod의 주기모드로 설정할경우 처음 그위치가 범위 안에 있으므로 
        //                              트리거 출력이 한번 발생한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmTriggerSetAbsPeriod(int nAxisNo, uint uMethod, double dPos);

        // 지정 축에 트리거 기능의 사용 여부, 출력 레벨, 위치 비교기, 트리거 신호 지속 시간 및 트리거 출력 모드를 반환한다.
        // 주의사항: IP에서는 AxmTriiggerSetBlock함수를 호출시 내부라이브러리에서 설정값이 ABS_POS_MODE로 사용하기 때문에 
        // 이함수를 반환하는값이 1로 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmTriggerGetAbsPeriod(int nAxisNo, ref uint upMethod, ref double dpPos);

        //  사용자가 지정한 시작위치부터 종료위치까지 일정구간마다 트리거를 출력 한다.
        [DllImport(LibraryFileName)]
        public static extern uint AxmTriggerSetBlock(int nAxisNo, double dStartPos, double dEndPos, double dPeriodPos);
        // 'AxmTriggerSetBlock' 함수의 트리거 설정한 값을 읽는다..
        [DllImport(LibraryFileName)]
        private static extern uint AxmTriggerGetBlock(int nAxisNo, ref double dpStartPos, ref double dpEndPos, ref double dpPeriodPos);
        // 사용자가 한 개의 트리거 펄스를 출력한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmTriggerOneShot(int nAxisNo);
        // 사용자가 한 개의 트리거 펄스를 몇초후에 출력한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmTriggerSetTimerOneshot(int nAxisNo, int mSec);
        // 절대위치 트리거 무한대 절대위치 출력한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmTriggerOnlyAbs(int nAxisNo, int nTrigNum, double[] dTrigPos);
        // 트리거 설정을 리셋한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmTriggerSetReset(int nAxisNo);

        #endregion

        #region CRC( 잔여 펄스 클리어 함수)

        //Level   : LOW(0), HIGH(1), UNUSED(2), USED(3)
        //uMethod : 잔여펄스 제거 출력 신호 펄스 폭 2 - 6까지 설정가능.
        //          0: Don't care , 1: Don't care, 2: 500 uSec, 3: 1 mSec, 4: 10 mSec, 5: 50 mSec, 6: 100 mSec

        //지정 축에 CRC 신호 사용 여부 및 출력 레벨을 설정한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmCrcSetMaskLevel(int nAxisNo, uint uLevel, uint uMethod);
        // 지정 축의 CRC 신호 사용 여부 및 출력 레벨을 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmCrcGetMaskLevel(int nAxisNo, ref uint upLevel, ref uint upMethod);

        //uOnOff  : CRC 신호를 Program으로 발생 여부  (FALSE(0),TRUE(1))

        // 지정 축에 CRC 신호를 강제로 발생 시킨다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmCrcSetOutput(int nAxisNo, uint uOnOff);
        // 지정 축의 CRC 신호를 강제로 발생 여부를 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmCrcGetOutput(int nAxisNo, ref uint upOnOff);

        //-----------	SMC-2V03 module 전용 함수 : EndLimit을 만날때 강제로 신호를 발생시킨다. --------
        // uPositiveUse : Positive Emeregency End limit에 대한 Clear출력 사용 레벨
        // uNegativeUse : Negative Emeregency End limit에 대한 Clear출력 사용 레벨
        // Level   : LOW(0), HIGH(1), UNUSED(2)
        // 지정 축에 리미트에 대한 CRC 신호의 사용 여부 및 출력 레벨을 설정한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmCrcSetEndLimit(int nAxisNo, uint uPositiveLevel, uint uNegativeLevel);
        // 지정 축의 리미트에 대한 CRC 신호의 사용 여부 및 출력 레벨을 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmCrcGetEndLimit(int nAxisNo, ref uint upPositiveLevel, ref uint upNegativeLevel);

        #endregion

        #region MPG(Manual Pulse Generation) 함수

        //================ SMC-2V03 module ===========================================================
        // lInputMethod : 0-7 까지 설정가능. 0:OnePhase, 1:TwoPhase1, 2:TwoPhase2, 3:TwoPhase4
        //                                   4:Level One Phase, 5:Level Two Phase1, 6: Level Two Phase2, 7:Level Two Phase4
        // lDriveMode   : 0-2 까지 설정가능 (0 :MPG 슬레이브 모드 ,1 :MPG PRESET 모드, 2 :MPG 연속 모드)
        // MPGPos		: MPG 입력신호마다 이동하는 거리
        // dMPGdenominator, dMPGnumerator 사용안함.


        //================ PCI-Nx04 ============================================================
        // lInputMethod : 0-3 까지 설정가능. 0:OnePhase, 1:TwoPhase1(IP만가능, QI지원안함) , 2:TwoPhase2, 3:TwoPhase4
        // lDriveMode   : 0만 설정가능 (0 :MPG 연속모드)
        // MPGPos		: MPG 입력신호마다 이동하는 거리
        // MPGdenominator: MPG(수동 펄스 발생 장치 입력)구동 시 나누기 값
        // dMPGnumerator : MPG(수동 펄스 발생 장치 입력)구동 시 곱하기 값
        // dwNumerator   : 최대(1 에서    64) 까지 설정 가능
        // dwDenominator : 최대(1 에서  4096) 까지 설정 가능
        // dMPGdenominator = 4096, MPGnumerator=1 가 의미하는 것은 
        // MPG 한바퀴에 200펄스면 그대로 1:1로 1펄스씩 출력을 의미한다. 
        // 만약 dMPGdenominator = 4096, MPGnumerator=2 로 했을경우는 1:2로 2펄스씩 출력을 내보낸다는의미이다. 
        // 여기에 MPG PULSE = ((Numerator) * (Denominator)/ 4096 ) 칩내부에 출력나가는 계산식이다.

        // 지정 축에 MPG 입력방식, 드라이브 구동 모드, 이동 거리, MPG 속도 등을 설정한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMPGSetEnable(int nAxisNo, int nInputMethod, int nDriveMode, double dMPGPos, double dVel, double dAccel);
        // 지정 축에 MPG 입력방식, 드라이브 구동 모드, 이동 거리, MPG 속도 등을 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMPGGetEnable(int nAxisNo, ref int npInputMethod, ref int npDriveMode, ref double dpMPGPos, ref double dpVel);

        // IP 사용안함, QI 전용 함수.
        // 지정 축에 MPG 드라이브 구동 모드에서 한펄스당 이동할 펄스 비율을 설정한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMPGSetRatio(int nAxisNo, double dMPGnumerator, double dMPGdenominator);
        // 지정 축에 MPG 드라이브 구동 모드에서 한펄스당 이동할 펄스 비율을 반환한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMPGGetRatio(int nAxisNo, ref double dMPGnumerator, ref double dMPGdenominator);

        // 지정 축에 MPG 드라이브 설정을 해지한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMPGReset(int nAxisNo);

        #endregion

        #region 헬리컬 이동  (PCI-Nx04 전용 함수)
        // 지정된 좌표계에 시작점, 종료점과 중심점을 지정하여 헬리컬 보간 구동하는 함수이다.
        // AxmContiBeginNode, AxmContiEndNode와 같이사용시 지정된 좌표계에 시작점, 종료점과 중심점을 지정하여 헬리컬 연속보간 구동하는 함수이다. 
        // 원호 연속 보간 구동을 위해 내부 Queue에 저장하는 함수이다. AxmContiStart함수를 사용해서 시작한다. (연속보간 함수와 같이 이용한다)
        // dCenterPos = 중심점 X,Y  , dEndPos = 종료점 X,Y 	
        // uCWDir   DIR_CCW(0): 반시계방향, DIR_CW(1) 시계방향	
        [DllImport(LibraryFileName)]
        private static extern uint AxmHelixCenterMove(int lCoord, double dCenterXPos, double dCenterYPos, double dEndXPos, double dEndYPos, double dZPos, double dVel, double dAccel, double dDecel, uint uCWDir);
        // 지정된 좌표계에 시작점, 종료점과 반지름을 지정하여 헬리컬 보간 구동하는 함수이다. 
        // AxmContiBeginNode, AxmContiEndNode와 같이사용시 지정된 좌표계에 중간점, 종료점을 지정하여 헬리컬연속 보간 구동하는 함수이다. 
        // 원호 연속 보간 구동을 위해 내부 Queue에 저장하는 함수이다. AxmContiStart함수를 사용해서 시작한다. (연속보간 함수와 같이 이용한다.)
        // dMidPos = 중간점 X,Y  , dEndPos = 종료점 X,Y 
        [DllImport(LibraryFileName)]
        private static extern uint AxmHelixPointMove(int lCoord, double dMidXPos, double dMidYPos, double dEndXPos, double dEndYPos, double dZPos, double dVel, double dAccel, double dDecel);
        // 지정된 좌표계에 시작점, 종료점과 반지름을 지정하여 헬리컬 보간 구동하는 함수이다.
        // AxmContiBeginNode, AxmContiEndNode와 같이사용시 지정된 좌표계에 시작점, 종료점과 반지름을 지정하여 헬리컬연속 보간 구동하는 함수이다. 
        // 원호 연속 보간 구동을 위해 내부 Queue에 저장하는 함수이다. AxmContiStart함수를 사용해서 시작한다. (연속보간 함수와 같이 이용한다.)
        // dRadius = 반지름, dEndPos = 종료점 X,Y  , uShortDistance = 작은원(0), 큰원(1)
        // uCWDir   DIR_CCW(0): 반시계방향, DIR_CW(1) 시계방향	
        [DllImport(LibraryFileName)]
        private static extern uint AxmHelixRadiusMove(int lCoord, double dRadius, double dEndXPos, double dEndYPos, double dZPos, double dVel, double dAccel, double dDecel, uint uCWDir, uint uShortDistance);
        // 지정된 좌표계에 시작점, 회전각도와 반지름을 지정하여 헬리컬 보간 구동하는 함수이다
        // AxmContiBeginNode, AxmContiEndNode와 같이사용시 지정된 좌표계에 시작점, 회전각도와 반지름을 지정하여 헬리컬연속 보간 구동하는 함수이다. 
        // 원호 연속 보간 구동을 위해 내부 Queue에 저장하는 함수이다. AxmContiStart함수를 사용해서 시작한다. (연속보간 함수와 같이 이용한다.)
        //dCenterPos = 중심점 X,Y  , dAngle = 각도.
        // uCWDir   DIR_CCW(0): 반시계방향, DIR_CW(1) 시계방향	
        [DllImport(LibraryFileName)]
        private static extern uint AxmHelixAngleMove(int lCoord, double dCenterXPos, double dCenterYPos, double dAngle, double dZPos, double dVel, double dAccel, double dDecel, uint uCWDir);
        #endregion

        #region 스플라인 이동 (PCI-Nx04 전용 함수)
        // AxmContiBeginNode, AxmContiEndNode와 같이사용안함. 
        // 스플라인 연속 보간 구동하는 함수이다. 원호 연속 보간 구동을 위해 내부 Queue에 저장하는 함수이다.
        // AxmContiStart함수를 사용해서 시작한다. (연속보간 함수와 같이 이용한다.)	
        // lPosSize : 최소 3개 이상.
        // 2축으로 사용시 dPoZ값을 0으로 넣어주면 됨.
        // 3축으로 사용시 축맵핑을 3개및 dPosZ 값을 넣어준다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmSplineWrite(int lCoord, int lPosSize, ref double dPosX, ref double dPosY, double dVel, double dAccel, double dDecel, double dPosZ, int lPointFactor);
        #endregion

        #region 스테이지 보정

        [DllImport("AXL.dll")] public static extern uint AxmCompensationTwoDimSet(int lTableNo, int lSourceAxis1, int lSourceAxis2, int lTargetAxis1, int lTargetAxis2, int lSize1, int lSize2, double[] dpMotorPosition1, double[] dpMotorPosition2, double[] dpLoadPosition1, double[] dpLoadPosition2);
        [DllImport("AXL.dll")] public static extern uint AxmCompensationTwoDimGet(int lTableNo, ref int lpSourceAxis1, ref int lpSourceAxis2, ref int lpTargetAxis1, ref int lpTargetAxis2, ref int lpSize1, ref int lpSize2, double[] dpMotorPosition1, double[] dpMotorPosition2, double[] dpLoadPosition1, double[] dpLoadPosition2);
        [DllImport("AXL.dll")] public static extern uint AxmCompensationTwoDimReset(int lTableNo);
        [DllImport("AXL.dll")] public static extern uint AxmCompensationTwoDimIsSet(int lTableNo, ref uint dwpSet);
        [DllImport("AXL.dll")] public static extern uint AxmCompensationTwoDimEnable(int lTableNo, uint dwEnable);
        [DllImport("AXL.dll")] public static extern uint AxmCompensationTwoDimIsEnable(int lTableNo, ref uint dwpEnable);

        #endregion
        //public static int CompensationTwoDimSet(int lTableNo, int lSourceAxis1, int lSourceAxis2, int lTargetAxis1, int lTargetAxis2, int lSize1, int lSize2, double[] dpMotorPosition1, double[] dpMotorPosition2, double[] dpLoadPosition1, double[] dpLoadPosition2)
        //{

        //}

        #endregion

        #region Field

        #endregion

        #region Method
        #region 모션 파라메타 설정
        public static int SetAccelerationUnit(int axis, AccelUnit mode)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmMotSetAccelUnit", AXM.AxmMotSetAccelUnit(axis, (uint)mode))) != 0) return ret;
            return ret;
        }

        public static int GetAccelerationUnit(int axis, ref AccelUnit mode)
        {
            int ret = 0;
            uint value = (uint)mode;
            if ((ret = AXL.CheckErrorCode("AXM.AxmMotGetAccelUnit", AXM.AxmMotGetAccelUnit(axis, ref value))) != 0) return ret;
            mode = (AccelUnit)value;
            return ret;
        }

        public static int GetOutputMethod(int axis, ref MotorOutputMethod method)
        {
            int ret = 0;
            uint value = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmMotGetPulseOutMethod", AXM.AxmMotGetPulseOutMethod(axis, ref value))) != 0) return ret;
            method = (MotorOutputMethod)value;
            return ret;
        }
        public static int SetOutputMethod(int axis, MotorOutputMethod method)
        {
            int ret = 0;
            uint value = (uint)method;
            if ((ret = AXL.CheckErrorCode("AXM.AxmMotSetPulseOutMethod", AXM.AxmMotSetPulseOutMethod(axis, value))) != 0) return ret;
            return ret;
        }

        public static int GetEncoderMethod(int axis, ref EncoderInputMethod method)
        {
            int ret = 0;
            uint value = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmMotGetEncInputMethod", AXM.AxmMotGetEncInputMethod(axis, ref value))) != 0) return ret;
            method = (EncoderInputMethod)value;
            return ret;
        }
        public static int SetEncoderMethod(int axis, EncoderInputMethod method)
        {
            int ret = 0;
            uint value = (uint)method;
            if ((ret = AXL.CheckErrorCode("AXM.AxmMotSetEncInputMethod", AXM.AxmMotSetEncInputMethod(axis, value))) != 0) return ret;
            return ret;
        }
        public static int SetMoveUnitPerPulse(int axis, int dUnit, int nPulse)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmMotSetMoveUnitPerPulse", AXM.AxmMotSetMoveUnitPerPulse(axis, dUnit, nPulse))) != 0) return ret;
            return ret;
        }
        public static int GetMoveUnitPerPulse(int axis,ref  double dUnit, ref int nPulse)
        {
            int ret = 0;
          
            if ((ret = AXL.CheckErrorCode("AXM.AxmMotSetMoveUnitPerPulse", AXM.AxmMotGetMoveUnitPerPulse(axis,ref dUnit,ref nPulse))) != 0) return ret;
            return ret;
        }
        public static int SetAbsRelMode(int axis, bool bAbs = true)
        {
            int ret = 0;
            uint nMode = 0;
            if(bAbs)
            {
                nMode = 0;
            }
            else
            {
                nMode = 1;
            }
            if ((ret = AXL.CheckErrorCode("AXM.AxmMotSetMoveUnitPerPulse", AXM.AxmMotSetAbsRelMode(axis, nMode))) != 0) return ret;
            return ret;
        }
        

        public static int SetProfileMode(int axis, AXT_MOTION_PROFILE_MODE mode)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmMotSetProfileMode", AXM.AxmMotSetProfileMode(axis, (uint)mode))) != 0) return ret;
            return ret;
        }

        public static int GetMaxVelocity(int axis, ref double velocity)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmMotGetMaxVel", AXM.AxmMotGetMaxVel(axis, ref velocity))) != 0) return ret;
            return ret;
        }
        public static int SetMaxVelocity(int axis, double velocity)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmMotSetMaxVel", AXM.AxmMotSetMaxVel(axis, velocity))) != 0) return ret;
            if ((ret = AXL.CheckErrorCode("AXM.AxmOverrideSetMaxVel", AXM.AxmOverrideSetMaxVel(axis, velocity))) != 0) return ret;
            return ret;
        }

        public static int SetAccelerationJerk(int axis, double accelerationJerk)
        {
            int ret = 0;

            if ((ret = AXL.CheckErrorCode("AXM.AxmMotSetAccelJerk", AXM.AxmMotSetAccelJerk(axis, accelerationJerk))) != 0) return ret;

            return ret;
        }

        public static int SetDecelerationJerk(int axis, double decelerationJerk)
        {
            int ret = 0;

            if ((ret = AXL.CheckErrorCode("AXM.AxmMotSetAccelJerk", AXM.AxmMotSetDecelJerk(axis, decelerationJerk))) != 0) return ret;

            return ret;
        }

        #endregion

        #region 입출력 신호 관련 설정함수
        public static int GetZPhaseLevel(int axis, ref ActiveLevel level)
        {
            int ret = 0;
            uint value = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalGetZphaseLevel", AXM.AxmSignalGetZphaseLevel(axis, ref value))) != 0) return ret;
            level = value == 0 ? ActiveLevel.Low : ActiveLevel.High;
            return ret;
        }
        public static int SetZPhaseLevel(int axis, ActiveLevel level)
        {
            int ret = 0;
            uint value = level == ActiveLevel.High ? (uint)1 : (uint)0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalSetZphaseLevel", AXM.AxmSignalSetZphaseLevel(axis, value))) != 0) return ret;
            return ret;
        }

        public static int GetAmpEnableLevel(int axis, ref ActiveLevel level)
        {
            int ret = 0;
            uint value = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalGetServoOnLevel", AXM.AxmSignalGetServoOnLevel(axis, ref value))) != 0) return ret;
            level = value == 0 ? ActiveLevel.Low : ActiveLevel.High;
            return ret;
        }
        public static int SetAmpEnableLevel(int axis, ActiveLevel level)
        {
            int ret = 0;
            uint value = level == ActiveLevel.High ? (uint)1 : (uint)0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalSetServoOnLevel", AXM.AxmSignalSetServoOnLevel(axis, value))) != 0) return ret;
            return ret;
        }

        public static int GetAmpEnabled(int axis, ref bool value)
        {
            int ret = 0;
            uint use = (uint)(value ? 1 : 0);
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalIsServoOn", AXM.AxmSignalIsServoOn(axis, ref use))) != 0) return ret;
            value = use == 1;
            return ret;
        }
        public static int SetAmpEnabled(int axis, bool value)
        {
            int ret = 0;
            uint use = (uint)(value ? 1 : 0);
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalServoOn", AXM.AxmSignalServoOn(axis, use))) != 0) return ret;
            return ret;
        }

        public static int GetAmpResetLevel(int axis, ref ActiveLevel level)
        {
            int ret = 0;
            uint value = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalGetServoAlarmResetLevel", AXM.AxmSignalGetServoAlarmResetLevel(axis, ref value))) != 0) return ret;
            level = value == 0 ? ActiveLevel.Low : ActiveLevel.High;
            return ret;
        }
        public static int SetAmpResetLevel(int axis, ActiveLevel level)
        {
            int ret = 0;
            uint value = level == ActiveLevel.High ? (uint)1 : (uint)0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalSetServoAlarmResetLevel", AXM.AxmSignalSetServoAlarmResetLevel(axis, value))) != 0) return ret;
            return ret;
        }

        public static int GetAmpFaultAction(int axis, ref MotorEventAction action)
        {
            int ret = 0;
            uint value = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalGetServoAlarm", AXM.AxmSignalGetServoAlarm(axis, ref value))) != 0) return ret;
            action = value == 2 ? MotorEventAction.Abort : MotorEventAction.None;
            return ret;
        }
        public static int SetAmpFaultAction(int axis, MotorEventAction action)
        {
            int ret = 0;
            uint value = 2;
            if (action != MotorEventAction.None) return ret;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalSetServoAlarm", AXM.AxmSignalSetServoAlarm(axis, value))) != 0) return ret;
            return ret;
        }
        public static int GetAmpFaultLevel(int axis, ref ActiveLevel level)
        {
            int ret = 0;
            uint value = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalGetServoAlarm", AXM.AxmSignalGetServoAlarm(axis, ref value))) != 0) return ret;
            level = value == 0 ? ActiveLevel.Low : ActiveLevel.High;
            return ret;
        }
        public static int SetAmpFaultLevel(int axis, ActiveLevel level)
        {
            int ret = 0;
            uint value = level == ActiveLevel.High ? (uint)1 : (uint)0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalSetServoAlarm", AXM.AxmSignalSetServoAlarm(axis, value))) != 0) return ret;
            return ret;
        }
        public static int GetAmpFaultValue(int axis, ref bool value)
        {
            int ret = 0;
            uint status = (uint)0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalReadServoAlarm", AXM.AxmSignalReadServoAlarm(axis, ref status))) != 0) return ret;
            value = status == (uint)1 ? true : false;
            return ret;
        }

        public static int GetInPositionEnable(int axis, ref bool enable)
        {
            int ret = 0;
            uint value = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalGetInpos", AXM.AxmSignalGetInpos(axis, ref value))) != 0) return ret;
            enable = value != 2;
            return ret;
        }
        public static int SetInPositionEnable(int axis, bool enable)
        {
            int ret = 0;
            uint value = 2;
            if (enable != false) return ret;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalSetInpos", AXM.AxmSignalSetInpos(axis, value))) != 0) return ret;
            return ret;
        }
        public static int GetInPositionLevel(int axis, ref ActiveLevel level)
        {
            int ret = 0;
            uint value = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalGetInpos", AXM.AxmSignalGetInpos(axis, ref value))) != 0) return ret;
            level = value == 0 ? ActiveLevel.Low : ActiveLevel.High;
            return ret;
        }
        public static int SetInPositionLevel(int axis, InPosition level)
        {
            int ret = 0;
            uint value = (uint)level;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalSetInpos", AXM.AxmSignalSetInpos(axis, value))) != 0) return ret;
            return ret;
        }
        public static int GetInPositionValue(int axis, ref bool value)
        {
            int ret = 0;
            uint status = (uint)(value ? 1 : 0);
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalSetInpos", AXM.AxmSignalReadInpos(axis, ref status))) != 0) return ret;
            value = status == (uint)1;
            return ret;
        }

        public static int GetNegativeLimitAction(int axis, ref MotorEventAction action)
        {
            int ret = 0;
            uint stop = 0, positive = 0, negative = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalGetLimit", AXM.AxmSignalGetLimit(axis, ref stop, ref positive, ref negative))) != 0) return ret;
            action = stop == 0 ? MotorEventAction.EmergencyStop : MotorEventAction.Stop;
            return ret;
        }
        public static int SetNegativeLimitAction(int axis, MotorEventAction action)
        {
            int ret = 0;
            uint stop = 0, positive = 0, negative = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalGetLimit", AXM.AxmSignalGetLimit(axis, ref stop, ref positive, ref negative))) != 0) return ret;
            stop = action == MotorEventAction.Stop ? (uint)1 : (uint)0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalSetLimit", AXM.AxmSignalSetLimit(axis, stop, positive, negative))) != 0) return ret;
            return ret;
        }
        public static int GetNegativeLimitLevel(int axis, ref ActiveLevel level)
        {
            int ret = 0;
            uint stop = 0, positive = 0, negative = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalGetLimit", AXM.AxmSignalGetLimit(axis, ref stop, ref positive, ref negative))) != 0) return ret;
            level = negative == 0 ? ActiveLevel.Low : ActiveLevel.High;
            return ret;
        }
        public static int SetNegativeLimitLevel(int axis, ActiveLevel level)
        {
            int ret = 0;
            uint stop = 0, positive = 0, negative = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalGetLimit", AXM.AxmSignalGetLimit(axis, ref stop, ref positive, ref negative))) != 0) return ret;
            negative = level == ActiveLevel.High ? (uint)1 : (uint)0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalSetLimit", AXM.AxmSignalSetLimit(axis, stop, positive, negative))) != 0) return ret;
            return ret;
        }
        public static int GetNegativeLimitValue(int axis, ref bool value)
        {
            int ret = 0;
            uint positive = 0, negative = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalReadLimit", AXM.AxmSignalReadLimit(axis, ref positive, ref negative))) != 0) return ret;
            value = negative == (uint)1 ? true : false;
            return ret;
        }
        public static int SetNegativeLimitNotUse(int axis)
        {
            int ret = 0;
            uint stop = 0, positive = 0, negative = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalGetLimit", AXM.AxmSignalGetLimit(axis, ref stop, ref positive, ref negative))) != 0) return ret;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalSetLimit", AXM.AxmSignalSetLimit(axis, stop, positive, (uint)2))) != 0) return ret;
            return ret;
        }

        public static int GetPositiveLimitAction(int axis, ref MotorEventAction action)
        {
            int ret = 0;
            uint stop = 0, positive = 0, negative = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalGetLimit", AXM.AxmSignalGetLimit(axis, ref stop, ref positive, ref negative))) != 0) return ret;
            action = stop == 0 ? MotorEventAction.EmergencyStop : MotorEventAction.Stop;
            return ret;
        }
        public static int SetPositiveLimitAction(int axis, MotorEventAction action)
        {
            int ret = 0;
            uint stop = 0, positive = 0, negative = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalGetLimit", AXM.AxmSignalGetLimit(axis, ref stop, ref positive, ref negative))) != 0) return ret;
            stop = action == MotorEventAction.None ? (uint)1 : (uint)0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalSetLimit", AXM.AxmSignalSetLimit(axis, stop, positive, negative))) != 0) return ret;
            return ret;
        }
        public static int GetPositiveLimitLevel(int axis, ref ActiveLevel level)
        {
            int ret = 0;
            uint stop = 0, positive = 0, negative = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalGetLimit", AXM.AxmSignalGetLimit(axis, ref stop, ref positive, ref negative))) != 0) return ret;
            level = positive == 0 ? ActiveLevel.Low : ActiveLevel.High;
            return ret;
        }
        public static int SetPositiveLimitLevel(int axis, ActiveLevel level)
        {
            int ret = 0;
            uint stop = 0, positive = 0, negative = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalGetLimit", AXM.AxmSignalGetLimit(axis, ref stop, ref positive, ref negative))) != 0) return ret;
            positive = level == ActiveLevel.High ? (uint)1 : (uint)0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalSetLimit", AXM.AxmSignalSetLimit(axis, stop, positive, negative))) != 0) return ret;
            return ret;
        }
        public static int SetPositiveLimitLevel(int axis, uint stopMode, ActiveLevel positive, ActiveLevel negetive)
        {
            int ret = 0;
            uint stop = 0, curPos = 0, curNeg = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalGetLimit", AXM.AxmSignalGetLimit(axis, ref stop, ref curPos, ref curNeg))) != 0) return ret;
            uint newPos = (positive == ActiveLevel.High) ? 1u : 0u;
            uint newNeg = (negetive == ActiveLevel.High) ? 1u : 0u;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalSetLimit", AXM.AxmSignalSetLimit(axis, stop, newPos, newNeg))) != 0) return ret;
            return ret;
        }
        public static int GetPositiveLimitValue(int axis, ref bool value)
        {
            int ret = 0;
            uint positive = 0, negative = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalReadLimit", AXM.AxmSignalReadLimit(axis, ref positive, ref negative))) != 0) return ret;
            value = positive == (uint)1 ? true : false;
            return ret;
        }
        public static int SetPositiveLimitNotUse(int axis)
        {
            int ret = 0;
            uint stop = 0, positive = 0, negative = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalGetLimit", AXM.AxmSignalGetLimit(axis, ref stop, ref positive, ref negative))) != 0) return ret;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalSetLimit", AXM.AxmSignalSetLimit(axis, stop, (uint)2, negative))) != 0) return ret;
            return ret;
        }

        public static int GetNegativePositionAction(int axis, ref MotorEventAction action)
        {
            int ret = 0;
            uint use = 0, stop = 0, mode = 0;
            double positive = 0, negative = 0;

            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalGetSoftLimit", AXM.AxmSignalGetSoftLimit(axis, ref use, ref stop, ref mode, ref positive, ref negative))) != 0) return ret;
            if (use == 0)
                action = MotorEventAction.None;
            else
            {
                if (stop == 0)
                    action = MotorEventAction.EmergencyStop;
                else
                    action = MotorEventAction.Stop;
            }

            return ret;
        }
        public static int SetNegativePositionAction(int axis, MotorEventAction action)
        {
            int ret = 0;
            uint use = 0, stop = 0, mode = 0;
            double positive = 0, negative = 0;

            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalGetSoftLimit", AXM.AxmSignalGetSoftLimit(axis, ref use, ref stop, ref mode, ref positive, ref negative))) != 0) return ret;
            use = action == MotorEventAction.None ? (uint)0 : (uint)1;
            stop = action == MotorEventAction.Stop ? (uint)1 : (uint)0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalSetSoftLimit", AXM.AxmSignalSetSoftLimit(axis, 0, stop, mode, positive, negative))) != 0) return ret;

            return ret;
        }
        public static int GetNegativePosition(int axis, ref double position)
        {
            int ret = 0;
            uint use = 0, stop = 0, mode = 0;
            double positive = 0, negative = 0;

            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalGetSoftLimit", AXM.AxmSignalGetSoftLimit(axis, ref use, ref stop, ref mode, ref positive, ref negative))) != 0) return ret;
            position = negative;

            return ret;
        }
        public static int SetNegativePosition(int axis, double position)
        {
            int ret = 0;
            uint use = 0, stop = 0, mode = 0;
            double positive = 0, negative = 0;

            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalGetSoftLimit", AXM.AxmSignalGetSoftLimit(axis, ref use, ref stop, ref mode, ref positive, ref negative))) != 0) return ret;
            negative = position;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalSetSoftLimit", AXM.AxmSignalSetSoftLimit(axis, 0, stop, mode, positive, negative))) != 0) return ret;

            return ret;
        }

        public static int GetPositivePositionAction(int axis, ref MotorEventAction action)
        {
            int ret = 0;
            uint use = 0, stop = 0, mode = 0;
            double positive = 0, negative = 0;

            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalGetSoftLimit", AXM.AxmSignalGetSoftLimit(axis, ref use, ref stop, ref mode, ref positive, ref negative))) != 0) return ret;
            if (use == 0)
                action = MotorEventAction.None;
            else
            {
                if (stop == 0)
                    action = MotorEventAction.EmergencyStop;
                else
                    action = MotorEventAction.Stop;
            }

            return ret;
        }
        public static int SetPositivePositionAction(int axis, MotorEventAction action)
        {
            int ret = 0;
            uint use = 0, stop = 0, mode = 0;
            double positive = 0, negative = 0;

            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalGetSoftLimit", AXM.AxmSignalGetSoftLimit(axis, ref use, ref stop, ref mode, ref positive, ref negative))) != 0) return ret;
            use = action == MotorEventAction.None ? (uint)0 : (uint)1;
            stop = action == MotorEventAction.Stop ? (uint)1 : (uint)0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalSetSoftLimit", AXM.AxmSignalSetSoftLimit(axis, 0, stop, mode, positive, negative))) != 0) return ret;

            return ret;
        }
        public static int GetPositivePosition(int axis, ref double position)
        {
            int ret = 0;
            uint use = 0, stop = 0, mode = 0;
            double positive = 0, negative = 0;

            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalGetSoftLimit", AXM.AxmSignalGetSoftLimit(axis, ref use, ref stop, ref mode, ref positive, ref negative))) != 0) return ret;
            position = positive;

            return ret;
        }
        public static int SetPositivePosition(int axis, double position)
        {
            int ret = 0;
            uint use = 0, stop = 0, mode = 0;
            double positive = 0, negative = 0;

            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalGetSoftLimit", AXM.AxmSignalGetSoftLimit(axis, ref use, ref stop, ref mode, ref positive, ref negative))) != 0) return ret;
            positive = position;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalSetSoftLimit", AXM.AxmSignalSetSoftLimit(axis, 0, stop, mode, positive, negative))) != 0) return ret;

            return ret;
        }

        public static int SetSignalStop(int axis, uint stopMode, uint level)
        {
            int ret = 0;

            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalSetStop", AXM.AxmSignalSetStop(axis, stopMode, level))) != 0) return ret;

            return ret;
        }


        public static int ReadInputBit(int axis, int bit, ref DioValue value)
        {
            int ret = 0;
            uint on = 0;
            value = DioValue.Unknown;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalReadInputBit", AXM.AxmSignalReadInputBit(axis, bit, ref on))) != 0) return ret;
            value = on == 0 ? DioValue.Off : DioValue.On;
            return ret;
        }

        public static int ReadOutputBit(int axis, int bit, ref DioValue value)
        {
            int ret = 0;
            uint on = 0;
            value = DioValue.Unknown;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalReadOutputBit", AXM.AxmSignalReadOutputBit(axis, bit, ref on))) != 0) return ret;
            value = on == 0 ? DioValue.Off : DioValue.On;
            return ret;
        }
        public static int WriteOutputBit(int axis, int bit, DioValue value)
        {
            int ret = 0;
            uint on = value == DioValue.On ? (uint)1 : (uint)0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalWriteOutputBit", AXM.AxmSignalWriteOutputBit(axis, bit, on))) != 0) return ret;
            return ret;
        }

        public static int GetHomeSensorLevel(int axis, ref ActiveLevel level)
        {
            int ret = 0;
            uint uLevel = 0;

            if ((ret = AXL.CheckErrorCode("AXM.GetHomeSensorLevel", AXM.AxmHomeGetSignalLevel(axis, ref uLevel))) != 0) return ret;
            level = uLevel == 0 ? ActiveLevel.Low : ActiveLevel.High;

            return ret;
        }
        public static int SetHomeSensorLevel(int axis, ActiveLevel level)
        {
            int ret = 0;
            uint uLevel = (uint)(level == ActiveLevel.Low ? 0 : 1);

            if ((ret = AXL.CheckErrorCode("AXM.AxmHomeSetSignalLevel", AXM.AxmHomeSetSignalLevel(axis, uLevel))) != 0) return ret;

            return ret;
        }
        public static int GetHomeSensorValue(int axis, ref bool value)
        {
            int ret = 0;
            uint upStatus = 0;

            if ((ret = AXL.CheckErrorCode("AXM.AxmHomeReadSignal", AXM.AxmHomeReadSignal(axis, ref upStatus))) != 0) return ret;
            value = upStatus == 0 ? false : true;

            return ret;
        }
        #endregion

        #region 모션 구동중 및 구동후에 상태 확인하는 함수
        public static int GetAxisState(int axis, ref AxisState state)
        {
            int ret = 0;
            uint drive = 0;
            bool bValue = false;

            if ((ret = AXL.CheckErrorCode("AXM.AxmStatusReadMotion", AXM.AxmStatusReadMotion(axis, ref drive))) != 0) return ret;
            if ((ret = AXM.GetAmpFaultValue(axis, ref bValue)) != 0) return ret;

            if (bValue)
                state = AxisState.Error;
            else
            {
                if ((drive & (uint)AXT_MOTION_QIDRIVE_STATUS.Busy) == (uint)AXT_MOTION_QIDRIVE_STATUS.Busy)
                    state = AxisState.Moving;
                else
                    state = AxisState.Idle;
                if ((drive & (uint)AXT_MOTION_QIDRIVE_STATUS.Decelerating) == (uint)AXT_MOTION_QIDRIVE_STATUS.Decelerating)
                    state = AxisState.Stopping;
            }

            return ret;
        }

        public static int GetAxisCount(out int axisCount)
        {
            axisCount = 0;
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmInfoGetAxisCount", AXM.AxmInfoGetAxisCount(ref axisCount))) != 0)
                return ret;
            return 0;
        }

        public static int GetActualPosition(int axis, ref double pulse)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmStatusGetActPos", AXM.AxmStatusGetActPos(axis, ref pulse))) != 0) return ret;
            return ret;
        }
        public static int SetActualPosition(int axis, double pulse)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmStatusSetActPos", AXM.AxmStatusSetActPos(axis, pulse))) != 0) return ret;
            return ret;
        }

        public static int GetCommandPosition(int axis, ref double pulse)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmStatusGetCmdPos", AXM.AxmStatusGetCmdPos(axis, ref pulse))) != 0) return ret;
            return ret;
        }
        public static int SetCommandPosition(int axis, double pulse)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmStatusSetCmdPos", AXM.AxmStatusSetCmdPos(axis, pulse))) != 0) return ret;
            return ret;
        }

        public static int GetPositionError(int axis, ref double pulse)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmStatusReadPosError", AXM.AxmStatusReadPosError(axis, ref pulse))) != 0) return ret;
            return ret;
        }

        public static int GetVelocity(int axis, ref double pulse)
        {
            int ret = 0;
            AXT_MOTION_QIDRIVE_STATUS status = AXT_MOTION_QIDRIVE_STATUS.Direction;
            if ((ret = AXL.CheckErrorCode("AXM.AxmStatusReadVel", AXM.AxmStatusReadVel(axis, ref pulse))) != 0) return ret;
            if ((ret = AXM.GetDriveStatus(axis, ref status)) != 0) return ret;
            if ((status & AXT_MOTION_QIDRIVE_STATUS.Direction) != 0x00000000)
                pulse *= -1.0;
            return ret;
        }

        public static int GetInMotion(int axis, ref bool value)
        {
            int ret = 0;
            uint inmotion = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmStatusReadInMotion", AXM.AxmStatusReadInMotion(axis, ref inmotion))) != 0) return ret;
            value = inmotion == 1;
            return ret;
        }

        public static int GetDriveStatus(int axis, ref AXT_MOTION_QIDRIVE_STATUS status)
        {
            int ret = 0;
            uint value = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmStatusReadMotion", AXM.AxmStatusReadMotion(axis, ref value))) != 0) return ret;
            status = (AXT_MOTION_QIDRIVE_STATUS)value;
            return ret;
        }
        public static int GetMotionInfo(int axis, ref MOTION_INFO info)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmStatusReadMotionInfo", AXM.AxmStatusReadMotionInfo(axis, ref info))) != 0) return ret;
            return ret;
        }

        public static int ReadTorque(int axis, ref double torque)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmStatusReadTorque", AXM.AxmStatusReadTorque(axis, ref torque))) != 0) return ret;
            return ret;
        }
        #endregion

        #region 홈관련 함수
        public static int SetHomeMethod(int axis, HomeDirection direction, HomeSignal signal, HomeZPhase zphase, double homeClearTime, double escapeDistance)
        {
            int ret = 0;
            int nHmDir = (int)direction;
            uint uHomeSignal = (uint)signal;
            uint uZphase = (uint)zphase;

            if ((ret = AXL.CheckErrorCode("AXM.AxmHomeSetMethod", AXM.AxmHomeSetMethod(axis, nHmDir, uHomeSignal, uZphase, homeClearTime, escapeDistance))) != 0) return ret;
            return ret;
        }

        public static int SetHomeVelocity(int axis, double firstSearchVelocity, double secondSearchVelocity, double lastVelocity, double indexSearchVelocity, double firstSearchAcc, double secondSearchAcc)
        {
            int ret = 0;
            //AccelUnit accelUnit = AccelUnit.UnitPerSec2;

            //// modified by LIM.WT 2020.01.19
            //if ((ret = AXM.GetAccelerationUnit(axis, ref accelUnit)) != 0) return ret;
            //if (accelUnit == AccelUnit.UnitPerSec2)
            //{
            if ((ret = AXL.CheckErrorCode("AXM.AxmHomeSetVel", AXM.AxmHomeSetVel(axis, firstSearchVelocity, secondSearchVelocity, lastVelocity, indexSearchVelocity, firstSearchAcc, secondSearchAcc))) != 0) return ret;
            //}
            //else
            //{
            //    double firstSearchAccTime = 0.0;
            //    double secondSearchAccTime = 0.0;

            //    firstSearchAccTime = Axis.ToAccelerationTime(firstSearchAcc, 0, firstSearchVelocity).TotalSeconds;
            //    secondSearchAccTime = Axis.ToAccelerationTime(secondSearchAcc, 0, secondSearchVelocity).TotalSeconds;

            //    if ((ret = AXL.CheckErrorCode("AXM.AxmHomeSetVel", AXM.AxmHomeSetVel(axis, firstSearchVelocity, secondSearchVelocity, lastVelocity, indexSearchVelocity, firstSearchAccTime, secondSearchAccTime))) != 0) return ret;
            //}

            return ret;
        }

        public static int GetHomeVelocity(int axis, ref double firstSearchVelocity, ref double secondSearchVelocity, ref double lastVelocity, ref double indexSearchVelocity, ref double firstSearchAcc, ref double secondSearchAcc)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmHomeGetVel", AXM.AxmHomeGetVel(axis, ref firstSearchVelocity, ref secondSearchVelocity, ref lastVelocity, ref indexSearchVelocity, ref firstSearchAcc, ref secondSearchAcc))) != 0) return ret;
            return ret;
        }

        public static int GetHomeMethod(int axis, ref HomeDirection direction, ref HomeSignal signal, ref HomeZPhase zphase, ref double homeClearTime, ref double escapeDistance)
        {
            int ret = 0;
            int nHmDir = 0;
            uint uHomeSignal = 0, uZphase = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmHomeGetMethod", AXM.AxmHomeGetMethod(axis, ref nHmDir, ref uHomeSignal, ref uZphase, ref homeClearTime, ref escapeDistance))) != 0) return ret;
            direction = (HomeDirection)nHmDir;
            signal = (HomeSignal)uHomeSignal;
            zphase = (HomeZPhase)uZphase;
            return ret;
        }

        public static int SetHomeStart(int axis)
        {
            int ret = 0;

            if ((ret = AXL.CheckErrorCode("AXM.AxmHomeSetStart", AXM.AxmHomeSetStart(axis))) != 0) return ret;

            return ret;
        }

        public static int GetHomeResult(int axis, ref AXT_MOTION_HOME_RESULT result)
        {
            int ret = 0;
            uint upHomeResult = (uint)AXT_MOTION_HOME_RESULT.HOME_SUCCESS;

            if ((ret = AXL.CheckErrorCode("AXM.AxmHomeGetResult", AXM.AxmHomeGetResult(axis, ref upHomeResult))) != 0) return ret;

            result = (AXT_MOTION_HOME_RESULT)upHomeResult;

            return ret;
        }

        public static int Reset(int axis)
        {
            uint result = (uint)AXT_FUNC_RESULT.AXT_RT_SUCCESS;
            result = AXM.AxmSignalServoAlarmReset(axis, 1);

            return (int)result;
        }

        public static int AlarmReset(int axis, bool OnOff)
        {
            uint result = (uint)AXT_FUNC_RESULT.AXT_RT_SUCCESS;

            if (OnOff)
                result = AXM.AxmSignalServoAlarmReset(axis, 1);
            else
                result = AXM.AxmSignalServoAlarmReset(axis, 0);

            return (int)result;
        }

        #endregion

        #region 위치구동함수
        public static int MovePosition(int axis, double position, double velocity, double acceleration, double deceleration)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmMoveStartPos", AXM.AxmMoveStartPos(axis, position, velocity, acceleration, deceleration))) != 0) return ret;
            //Log.Write("AjinTest", string.Format("Move Position in Acceleration {0}, {1},{2},{3}",axis.Configuration.No, velocity, acceleration,deceleration));
            return ret;
        }

        public static int MovePosition(int axis, double position, double velocity, TimeSpan accelerationTime, TimeSpan decelerationTime)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmMoveStartPos", AXM.AxmMoveStartPos(axis, position, velocity, accelerationTime.TotalSeconds, decelerationTime.TotalSeconds))) != 0) return ret;
            //Log.Write("AjinTest", string.Format("Move Position in Acceleration Time {0}, {1},{2},{3}", axis.Configuration.No, velocity, acceleationTime.TotalSeconds, decelerationTime.TotalSeconds));
            return ret;
        }

        public static int MoveVelocity(int axis, double velocity, double acceleration, double deceleration)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmMoveVel", AXM.AxmMoveVel(axis, velocity, acceleration, deceleration))) != 0) return ret;
            //Log.Write("AjinTest", "Move Velocity in Acceleration");
            return ret;
        }

        public static int MoveVelocity(int axis, double velocity, TimeSpan accelerationTime, TimeSpan decelerationTime)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmMoveVel", AXM.AxmMoveVel(axis, velocity, accelerationTime.Seconds, decelerationTime.Seconds))) != 0) return ret;
            //Log.Write("AjinTest", "Move Velocity in Acceleration Time");
            return ret;
        }

        public static int MovePositionWithList(int axis, double position, double[] velocities, double[] accelerations, double[] decelerations)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmMoveStartPosWithList", AXM.AxmMoveStartPosWithList(axis, position, ref velocities[0], ref accelerations[0], ref decelerations[0], velocities.Length))) != 0) return ret;
            return ret;
        }

        public static int SearchSignal(int axis, double velocity, double acceleration, AXT_MOTION_HOME_DETECT_SIGNAL signal, AXT_MOTION_EDGE edge, AXT_MOTION_STOPMODE stop)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmMoveSignalSearch", AXM.AxmMoveSignalSearch(axis, velocity, acceleration, (int)signal, (int)edge, (int)stop))) != 0) return ret;
            return ret;
        }
        public static int SearchSignalCapture(int axis, double velocity, double acceleration, AXT_MOTION_HOME_DETECT_SIGNAL signal, AXT_MOTION_EDGE edge, AXT_MOTION_SELECTION target, AXT_MOTION_STOPMODE stop)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmMoveSignalCapture", AXM.AxmMoveSignalCapture(axis, velocity, acceleration, (int)signal, (int)edge, (int)target, (int)stop))) != 0) return ret;
            return ret;
        }
        public static int GetCapturePosition(int axis, ref double position)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmMoveGetCapturePos", AXM.AxmMoveGetCapturePos(axis, ref position))) != 0) return ret;
            return ret;
        }

        public static int Stop(int axis, double decel)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmMoveStop", AXM.AxmMoveStop(axis, decel))) != 0) return ret;
            return ret;
        }
        public static int StopEmergency(int axis)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmMoveEStop", AXM.AxmMoveEStop(axis))) != 0) return ret;
            return ret;
        }
        public static int StopSlowly(int axis)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmMoveSStop", AXM.AxmMoveSStop(axis))) != 0) return ret;
            return ret;
        }
        #endregion

        #region 오버라이드 함수
        public static int ModifyPosition(int axis, double position, double velocity, double acceleration, double deceleration)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmOverridePos", AXM.AxmOverridePos(axis, position))) != 0) return ret;
            if ((ret = AXL.CheckErrorCode("AXM.AxmOverrideAccelVelDecel", AXM.AxmOverrideAccelVelDecel(axis, velocity, acceleration, deceleration))) != 0) return ret;
            return ret;
        }
        public static int ModifyVelocity(int axis, double velocity, double acceleration, double deceleration)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmOverrideAccelVelDecel", AXM.AxmOverrideAccelVelDecel(axis, velocity, acceleration, deceleration))) != 0) return ret;
            return ret;
        }
        #endregion

        #region 겐트리 관련 함수
        public static int SetGantryEnable(int masterAxisNo, int slaveAxisNo, GantryHomingMethods gantryHomeMethod, double slaveOffset, double slaveOffsetRange)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmGantrySetEnable", AXM.AxmGantrySetEnable(masterAxisNo, slaveAxisNo, (uint)gantryHomeMethod, slaveOffset, slaveOffsetRange))) != 0) return ret;
            return ret;
        }
        public static int GetGantryEnable(int masterAxisNo, ref GenericUriParserOptions gantryHomeMethod, ref double slaveOffset, ref double slaveOffsetRange, ref bool gantryOn)
        {
            int ret = 0;
            uint upSlHomeUse = (uint)gantryHomeMethod;
            uint uGatryOn = (uint)(gantryOn == true ? 1 : 0);
            if ((ret = AXL.CheckErrorCode("AXM.AxmGantryGetEnable", AXM.AxmGantryGetEnable(masterAxisNo, ref upSlHomeUse, ref slaveOffset, ref slaveOffsetRange, ref uGatryOn))) != 0) return ret;
            gantryHomeMethod = (GenericUriParserOptions)upSlHomeUse;
            gantryOn = (bool)(uGatryOn == 0 ? false : true);
            return ret;
        }
        public static int SetGantryDisable(int masterAxisNo, int slaveAxisNo)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmGantrySetDisable", AXM.AxmGantrySetDisable(masterAxisNo, slaveAxisNo))) != 0) return ret;
            return ret;
        }
        #endregion

        #region 다축 구동 및 보간 구동 함수
        public static int MoveMultiplePosition(int[] axes, double[] position, double[] velocity, double[] acceleration, double[] deceleration)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmMoveStartMultiPos", AXM.AxmMoveStartMultiPos(axes.Length, ref axes[0], ref position[0], ref velocity[0], ref acceleration[0], ref deceleration[0]))) != 0) return ret;
            return ret;
        }

        public static int MoveLine(int coordinate, int[] axes, double[] position, double velocity, double acceleration, double deceleration)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmLineMove", AXM.AxmLineMove(coordinate, ref position[0], velocity, acceleration, deceleration))) != 0) return ret;
            return ret;
        }

        public static int MoveArcRadius(int coordinate, int[] axes, double[] endPosition, double radius, double velocity, double acceleration, double deceleration, AXT_MOTION_MOVE_DIR direction, AXT_MOTION_RADIUS_DISTANCE shortDistance)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmCircleRadiusMove", AXM.AxmCircleRadiusMove(coordinate, ref axes[0], radius, ref endPosition[0], velocity, acceleration, deceleration, (uint)direction, (uint)shortDistance))) != 0) return ret;
            return ret;
        }

        public static int MoveArcAngle(int coordinate, int[] axes, double[] centerPosition, double angle, double velocity, double acceleration, double deceleration, AXT_MOTION_MOVE_DIR direction)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmCircleAngleMove", AXM.AxmCircleAngleMove(coordinate, ref axes[0], ref centerPosition[0], angle, velocity, acceleration, deceleration, (uint)direction))) != 0) return ret;
            return ret;
        }

        public static int MoveArcEndPoint(int coordinate, int[] axes, double[] centerPosition, double[] endPosition, double velocity, double acceleration, double deceleration, AXT_MOTION_MOVE_DIR direction)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmCircleCenterMove", AXM.AxmCircleCenterMove(coordinate, ref axes[0], ref centerPosition[0], ref endPosition[0], velocity, acceleration, deceleration, (uint)direction))) != 0) return ret;
            return ret;
        }
        #endregion

        #region 연속 보간 설정 및 구동 함수
        public static int SetPathAxisMap(int coordinate, int[] axes)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmContiSetAxisMap", AXM.AxmContiSetAxisMap(coordinate, (uint)axes.Length, ref axes[0]))) != 0) return ret;
            return ret;
        }

        public static int SetPathAbsRelMode(int coordinate, AXT_MOTION_ABSREL mode)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmContiSetAbsRelMode", AXM.AxmContiSetAbsRelMode(coordinate, (uint)mode))) != 0) return ret;
            return ret;
        }

        public static int ClearPath(int coordinate)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmContiWriteClear", AXM.AxmContiWriteClear(coordinate))) != 0) return ret;
            return ret;
        }

        public static int IsPathMoving(int coordinate, ref bool value)
        {
            int ret = 0;

            uint isPath = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmContiIsMotion", AXM.AxmContiIsMotion(coordinate, ref isPath))) != 0) return ret;
            if (isPath == 0) value = false;
            else value = true;

            return ret;
        }

        public static int GetPathStep(int coordinate, ref int value)
        {
            int ret = 0;
            value = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmContiGetNodeNum", AXM.AxmContiGetNodeNum(coordinate, ref value))) != 0) return ret;
            return ret;
        }

        public static int GetPathTotalStep(int coordinate, ref int value)
        {
            int ret = 0;
            value = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmContiGetTotalNodeNum", AXM.AxmContiGetTotalNodeNum(coordinate, ref value))) != 0) return ret;
            return ret;
        }

        public static int GetPathBufferCount(int coordinate, ref int count)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmContiReadIndex", AXM.AxmContiReadIndex(coordinate, ref count))) != 0) return ret;
            return ret;
        }

        public static int BeginPath(int coordinate)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmContiBeginNode", AXM.AxmContiBeginNode(coordinate))) != 0) return ret;
            return ret;
        }

        public static int EndPath(int coordinate)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmContiEndNode", AXM.AxmContiEndNode(coordinate))) != 0) return ret;
            return ret;
        }

        public static int StartPath(int coordinate, uint profile, int angle)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmContiStart", AXM.AxmContiStart(coordinate, profile, angle))) != 0) return ret;
            return ret;
        }
        #endregion

        #region 인터럽트 함수
        public static int InterruptSetAxis(int axisNo, uint hwnd, uint message, CAXHS.AXT_INTERRUPT_PROC proc, ref uint pEvent)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmInterruptSetAxis", AXM.AxmInterruptSetAxis(axisNo, hwnd, message, proc, ref pEvent))) != 0) return ret;
            return ret;
        }

        public static int InterruptSetAxisEnable(int axisNo, uint use)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmInterruptSetAxisEnable", AXM.AxmInterruptSetAxisEnable(axisNo, use))) != 0) return ret;
            return ret;
        }

        public static int InterruptGetAxisEnable(int axisNo, ref uint upUse)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmInterruptGetAxisEnable", AXM.AxmInterruptGetAxisEnable(axisNo, ref upUse))) != 0) return ret;
            return ret;
        }

        public static int InterruptRead(ref int axisNo, ref uint flag)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmInterruptRead", AXM.AxmInterruptRead(ref axisNo, ref flag))) != 0) return ret;
            return ret;
        }

        public static int InterruptReadAxisFlag(int axisNo, int bank, ref uint flag)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmInterruptReadAxisFlag", AXM.AxmInterruptReadAxisFlag(axisNo, bank, ref flag))) != 0) return ret;
            return ret;
        }

        public static int InterruptSetUserEnable(int axisNo, int bank, uint interruptNum)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmInterruptSetUserEnable", AXM.AxmInterruptSetUserEnable(axisNo, bank, interruptNum))) != 0) return ret;
            return ret;
        }

        public static int InterruptGetUserEnable(int axisNo, int bank, ref uint interruptNum)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmInterruptGetUserEnable", AXM.AxmInterruptGetUserEnable(axisNo, bank, ref interruptNum))) != 0) return ret;
            return ret;
        }
        #endregion

        #region 트리거 함수
        public static int GetTriggerTimeLevel(int axisNo, ref double time, ref uint level, ref uint select, ref uint interrupt)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmTriggerGetTimeLevel", AXM.AxmTriggerGetTimeLevel(axisNo, ref time, ref level, ref select, ref interrupt))) != 0) return ret;
            return ret;
        }
        public static int SetTriggerTimeLevel(int axisNo, double time, uint level, uint select, uint interrupt)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmTriggerSetTimeLevel", AXM.AxmTriggerSetTimeLevel(axisNo, time, level, select, interrupt))) != 0) return ret;
            return ret;
        }

        public static int SetTriggerOnlyAbs(int axisNo, double[] position)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmTriggerOnlyAbs", AXM.AxmTriggerOnlyAbs(axisNo, position.Length, position))) != 0) return ret;
            //if ((ret = AXL.CheckErrorCode("AXM.AxmTriggerOnlyAbs", AXM.AxmTriggerOnlyAbs(axisNo, position.Length, ref position[0]))) != 0) return ret;
            return ret;
        }
        #endregion
        #endregion
    }
}