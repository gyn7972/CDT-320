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
            ObverseUpDownMode,   // ������ Up/Down
            ObverseSqr1Mode,         // ������ 1ü��
            ObverseSqr2Mode,         // ������ 2ü��
            ObverseSqr4Mode,         // ������ 4ü��
            ReverseUpDownMode,   // ������ Up/Down
            ReverseSqr1Mode,         // ������ 1ü��
            ReverseSqr2Mode,         // ������ 2ü��
            ReverseSqr4Mode,         // ������ 4ü��
        }
        #endregion

        #region Dll Imports

        #region ���� �� ��� Ȯ���Լ�(Info) - Infomation

        // �ش� ���� �����ȣ, ��� ��ġ, ��� ���̵� ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmInfoGetAxis(int nAxisNo, ref int lpNodeNum, ref int npModulePos, ref uint upModuleID);
        // ��� ����� �����ϴ��� ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmInfoIsMotionModule(ref uint upStatus);
        // �ش� ���� ��ȿ���� ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmInfoIsInvalidAxisNo(int lAxisNo);
        // CAMC-QI �� ����, �ý��ۿ� ������ ��ȿ�� ��� ����� ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmInfoGetAxisCount(ref int lpAxisCount);
        // �ش� ���/����� ù��° ���ȣ�� ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmInfoGetFirstAxisNo(int lNodeNum, int lModulePos, ref int lpAxisNo);

        #endregion

        #region ���� �� �Լ�

        // �ʱ� ���¿��� AXM ��� �Լ��� ���ȣ ������ 0 ~ (���� �ý��ۿ� ������ ��� - 1) �������� ��ȿ������
        // �� �Լ��� ����Ͽ� ���� ������ ���ȣ ��� ������ ���ȣ�� �ٲ� �� �ִ�.
        // �� �Լ��� ���� �ý����� H/W ������� �߻��� ���� ���α׷��� �Ҵ�� ���ȣ�� �״�� �����ϰ� ���� ���� ���� 
        // �������� ��ġ�� �����Ͽ� ����� ���� ������� �Լ��̴�.
        // ���ǻ��� : ���� ���� ���� ���ȣ�� ���Ͽ� ���� ��ȣ�� ���� ���� �ߺ��ؼ� ������ ��� 
        //            ���� ���ȣ�� ���� �ุ ���� ���ȣ�� ���� �� �� ������, 
        //            ������ ���� ������ ��ȣ�� ���ε� ���� ��� �Ұ����� ��찡 �߻� �� �� �ִ�.

        // �������� �����Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmVirtualSetAxisNoMap(int nRealAxisNo, int nVirtualAxisNo);
        // ������ ������ ��ȣ�� ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmVirtualGetAxisNoMap(int nRealAxisNo, ref int npVirtualAxisNo);
        // ��Ƽ �������� �����Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmVirtualSetMultiAxisNoMap(int nSize, ref int npRealAxesNo, ref int npVirtualAxesNo);
        // ������ ��Ƽ ������ ��ȣ�� ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmVirtualGetMultiAxisNoMap(int nSize, ref int npRealAxesNo, ref int npVirtualAxesNo);
        // ������ ������ �����Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmVirtualResetAxisMap();

        #endregion

        #region ���ͷ�Ʈ ���� �Լ�
        // �ݹ� �Լ� ����� �̺�Ʈ �߻� ������ ��� �ݹ� �Լ��� ȣ�� ������ ���� ������ �̺�Ʈ�� �������� �� �ִ� ������ ������
        // �ݹ� �Լ��� ������ ���� �� ������ ���� ���μ����� ��ü�Ǿ� �ְ� �ȴ�.
        // ��, �ݹ� �Լ� ���� ���ϰ� �ɸ��� �۾��� ���� ��쿡�� ��뿡 ���Ǹ� ���Ѵ�. 
        // �̺�Ʈ ����� ��������� �̿��Ͽ� ���ͷ�Ʈ �߻����θ� ���������� �����ϰ� �ִٰ� ���ͷ�Ʈ�� �߻��ϸ� 
        // ó�����ִ� �������, ������ ������ ���� �ý��� �ڿ��� �����ϰ� �ִ� ������ ������
        // ���� ������ ���ͷ�Ʈ�� �����ϰ� ó������ �� �ִ� ������ �ִ�.
        // �Ϲ������δ� ���� ������ ������, ���ͷ�Ʈ�� ����ó���� �ֿ� ���ɻ��� ��쿡 ���ȴ�. 
        // �̺�Ʈ ����� �̺�Ʈ�� �߻� ���θ� �����ϴ� Ư�� �����带 ����Ͽ� ���� ���μ����� ������ ���۵ǹǷ�
        // MultiProcessor �ý��۵�� �ڿ��� ���� ȿ�������� ����� �� �ְ� �Ǿ� Ư�� �����ϴ� ����̴�.

        // ���ͷ�Ʈ �޽����� �޾ƿ��� ���Ͽ� ������ �޽��� �Ǵ� �ݹ� �Լ��� ����Ѵ�.
        // (�޽��� �ڵ�, �޽��� ID, �ݹ��Լ�, ���ͷ�Ʈ �̺�Ʈ)
        //    hWnd    : ������ �ڵ�, ������ �޼����� ������ ���. ������� ������ NULL�� �Է�.
        //    wMsg    : ������ �ڵ��� �޼���, ������� �ʰų� ����Ʈ���� ����Ϸ��� 0�� �Է�.
        //    proc    : ���ͷ�Ʈ �߻��� ȣ��� �Լ��� ������, ������� ������ NULL�� �Է�.
        //    pEvent  : �̺�Ʈ ������� �̺�Ʈ �ڵ�
        [DllImport(LibraryFileName)]
        private static extern uint AxmInterruptSetAxis(int nAxisNo, uint hWnd, uint uMessage, CAXHS.AXT_INTERRUPT_PROC pProc, ref uint pEvent);

        // ���� ���� ���ͷ�Ʈ ��� ���θ� �����Ѵ�
        // �ش� �࿡ ���ͷ�Ʈ ���� / Ȯ��
        // uUse : ��� ���� => DISABLE(0), ENABLE(1)
        [DllImport(LibraryFileName)]
        private static extern uint AxmInterruptSetAxisEnable(int nAxisNo, uint uUse);
        // ���� ���� ���ͷ�Ʈ ��� ���θ� ��ȯ�Ѵ�
        [DllImport(LibraryFileName)]
        private static extern uint AxmInterruptGetAxisEnable(int nAxisNo, ref uint upUse);

        //���ͷ�Ʈ�� �̺�Ʈ ������� ����� ��� �ش� ���ͷ�Ʈ ���� �д´�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmInterruptRead(ref int npAxisNo, ref uint upFlag);

        // �ش� ���� ���ͷ�Ʈ �÷��� ���� ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmInterruptReadAxisFlag(int nAxisNo, int nBank, ref uint upFlag);

        // ���� ���� ����ڰ� ������ ���ͷ�Ʈ �߻� ���θ� �����Ѵ�.
        // lBank         : ���ͷ�Ʈ ��ũ ��ȣ (0 - 1) ��������.
        // uInterruptNum : ���ͷ�Ʈ ��ȣ ���� ��Ʈ��ȣ�� ���� hex�� Ȥ�� define�Ȱ��� ����
        // AXHS.h���Ͽ� IP, QI INTERRUPT_BANK1, 2 DEF�� Ȯ���Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmInterruptSetUserEnable(int nAxisNo, int lBank, uint uInterruptNum);

        // ���� ���� ����ڰ� ������ ���ͷ�Ʈ �߻� ���θ� Ȯ���Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmInterruptGetUserEnable(int nAxisNo, int lBank, ref uint upInterruptNum);

        #endregion

        #region ��� �Ķ��Ÿ ����
        // AxmMotLoadParaAll�� ������ Load ��Ű�� ������ �ʱ� �Ķ��Ÿ ������ �⺻ �Ķ��Ÿ ����. 
        // ���� PC�� ���Ǵ� ����࿡ �Ȱ��� ����ȴ�. �⺻�Ķ��Ÿ�� �Ʒ��� ����. 
        // 00:AXIS_NO.             =0       01:PULSE_OUT_METHOD.    =4      02:ENC_INPUT_METHOD.    =3     03:INPOSITION.          =2
        // 04:ALARM.               =0       05:NEG_END_LIMIT.       =0      06:POS_END_LIMIT.       =0     07:MIN_VELOCITY.        =1
        // 08:MAX_VELOCITY.        =700000  09:HOME_SIGNAL.         =4      10:HOME_LEVEL.          =1     11:HOME_DIR.            =0
        // 12:ZPHASE_LEVEL.        =1       13:ZPHASE_USE.          =0      14:STOP_SIGNAL_MODE.    =0     15:STOP_SIGNAL_LEVEL.   =0
        // 16:HOME_FIRST_VELOCITY. =10000   17:HOME_SECOND_VELOCITY.=10000  18:HOME_THIRD_VELOCITY. =2000  19:HOME_LAST_VELOCITY.  =100
        // 20:HOME_FIRST_ACCEL.    =40000   21:HOME_SECOND_ACCEL.   =40000  22:HOME_END_CLEAR_TIME. =1000  23:HOME_END_OFFSET.     =0
        // 24:NEG_SOFT_LIMIT.      =0.000   25:POS_SOFT_LIMIT.      =0      26:MOVE_PULSE.          =1     27:MOVE_UNIT.           =1
        // 28:INIT_POSITION.       =1000    29:INIT_VELOCITY.       =200    30:INIT_ACCEL.          =400   31:INIT_DECEL.          =400
        // 32:INIT_ABSRELMODE.     =0       33:INIT_PROFILEMODE.    =4

        // 00=[AXIS_NO             ]: �� (0�� ���� ������)
        // 01=[PULSE_OUT_METHOD    ]: Pulse out method TwocwccwHigh = 6
        // 02=[ENC_INPUT_METHOD    ]: disable = 0   1ü�� = 1  2ü�� = 2  4ü�� = 3, �ἱ ���ù��� ��ü��(-).1ü�� = 11  2ü�� = 12  4ü�� = 13
        // 03=[INPOSITION          ], 04=[ALARM     ], 05,06 =[END_LIMIT   ]  : 0 = A���� 1= B���� 2 = ������. 3 = �������� ����
        // 07=[MIN_VELOCITY        ]: ���� �ӵ�(START VELOCITY)
        // 08=[MAX_VELOCITY        ]: ����̹��� ������ �޾Ƶ��ϼ� �ִ� ���� �ӵ�. ���� �Ϲ� Servo�� 700k
        // Ex> screw : 20mm pitch drive: 10000 pulse ����: 400w
        // 09=[HOME_SIGNAL         ]: 4 - Home in0 , 0 :PosEndLimit , 1 : NegEndLimit // _HOME_SIGNAL����.
        // 10=[HOME_LEVEL          ]: 0 = A���� 1= B���� 2 = ������. 3 = �������� ����
        // 11=[HOME_DIR            ]: Ȩ ����(HOME DIRECTION) 1:+����, 0:-����
        // 12=[ZPHASE_LEVEL        ]: 0 = A���� 1= B���� 2 = ������. 3 = �������� ����
        // 13=[ZPHASE_USE          ]: Z���뿩��. 0: ������ , 1: +����, 2: -���� 
        // 14=[STOP_SIGNAL_MODE    ]: ESTOP, SSTOP ���� ��� 0:��������, 1:������ 
        // 15=[STOP_SIGNAL_LEVEL   ]: ESTOP, SSTOP ��� ����.  0 = A���� 1= B���� 2 = ������. 3 = �������� ���� 
        // 16=[HOME_FIRST_VELOCITY ]: 1�������ӵ� 
        // 17=[HOME_SECOND_VELOCITY]: �����ļӵ� 
        // 18=[HOME_THIRD_VELOCITY ]: ������ �ӵ� 
        // 19=[HOME_LAST_VELOCITY  ]: index�˻��� �����ϰ� �˻��ϱ����� �ӵ�. 
        // 20=[HOME_FIRST_ACCEL    ]: 1�� ���ӵ� , 21=[HOME_SECOND_ACCEL   ] : 2�� ���ӵ� 
        // 22=[HOME_END_CLEAR_TIME ]: ���� �˻� Enc �� Set�ϱ� ���� ���ð�,  23=[HOME_END_OFFSET] : ���������� Offset��ŭ �̵�.
        // 24=[NEG_SOFT_LIMIT      ]: - SoftWare Limit ���� �����ϸ� ������, 25=[POS_SOFT_LIMIT ]: + SoftWare Limit ���� �����ϸ� ������.
        // 26=[MOVE_PULSE          ]: ����̹��� 1ȸ���� �޽���              , 27=[MOVE_UNIT  ]: ����̹� 1ȸ���� �̵��� ��:��ũ�� Pitch
        // 28=[INIT_POSITION       ]: ������Ʈ ���� �ʱ���ġ  , ����ڰ� ���Ƿ� ��밡��
        // 29=[INIT_VELOCITY       ]: ������Ʈ ���� �ʱ�ӵ�  , ����ڰ� ���Ƿ� ��밡��
        // 30=[INIT_ACCEL          ]: ������Ʈ ���� �ʱⰡ�ӵ�, ����ڰ� ���Ƿ� ��밡��
        // 31=[INIT_DECEL          ]: ������Ʈ ���� �ʱⰨ�ӵ�, ����ڰ� ���Ƿ� ��밡��
        // 32=[INIT_ABSRELMODE     ]: ����(0)/���(1) ��ġ ����
        // 33=[INIT_PROFILEMODE    ]: �������ϸ��(0 - 4) ���� ����
        //                            '0': ��Ī Trapezode, '1': ���Ī Trapezode, '2': ��Ī Quasi-S Curve, '3':��Ī S Curve, '4':���Ī S Curve

        // AxmMotSaveParaAll�� ���� �Ǿ��� .mot������ �ҷ��´�. �ش� ������ ����ڰ� Edit �Ͽ� ��� �����ϴ�.
        [DllImport(LibraryFileName)]
        public static extern uint AxmMotLoadParaAll(string szFilePath);
        // ����࿡ ���� ��� �Ķ��Ÿ�� �ະ�� �����Ѵ�. .mot���Ϸ� �����Ѵ�.
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

        // �Ķ��Ÿ 28 - 31������ ����ڰ� ���α׷�������  �� �Լ��� �̿��� ���� �Ѵ�
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotSetParaLoad(int nAxisNo, double InitPos, double InitVel, double InitAccel, double InitDecel);
        // �Ķ��Ÿ 28 - 31������ ����ڰ� ���α׷�������  �� �Լ��� �̿��� Ȯ�� �Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotGetParaLoad(int nAxisNo, ref double InitPos, ref double InitVel, ref double InitAccel, ref double InitDecel);

        // ���� ���� �޽� ��� ����� �����Ѵ�.
        // uMethod  0 :OneHighLowHigh, 1 :OneHighHighLow, 2 :OneLowLowHigh, 3 :OneLowHighLow, 4 :TwoCcwCwHigh
        //          5 :TwoCcwCwLow, 6 :TwoCwCcwHigh, 7 :TwoCwCcwLow, 8 :TwoPhase, 9 :TwoPhaseReverse
        // OneHighLowHigh   = 0x0      // 1�޽� ���, PULSE(Active High), ������(DIR=Low)  / ������(DIR=High)
        // OneHighHighLow   = 0x1      // 1�޽� ���, PULSE(Active High), ������(DIR=High) / ������(DIR=Low)
        // OneLowLowHigh    = 0x2      // 1�޽� ���, PULSE(Active Low),  ������(DIR=Low)  / ������(DIR=High)
        // OneLowHighLow    = 0x3      // 1�޽� ���, PULSE(Active Low),  ������(DIR=High) / ������(DIR=Low)
        // TwoCcwCwHigh     = 0x4      // 2�޽� ���, PULSE(CCW:������),  DIR(CW:������),  Active High     
        // TwoCcwCwLow      = 0x5      // 2�޽� ���, PULSE(CCW:������),  DIR(CW:������),  Active Low     
        // TwoCwCcwHigh     = 0x6      // 2�޽� ���, PULSE(CW:������),   DIR(CCW:������), Active High
        // TwoCwCcwLow      = 0x7      // 2�޽� ���, PULSE(CW:������),   DIR(CCW:������), Active Low
        // TwoPhase         = 0x8      // 2��(90' ������),  PULSE lead DIR(CW: ������), PULSE lag DIR(CCW:������)
        // TwoPhaseReverse  = 0x9      // 2��(90' ������),  PULSE lead DIR(CCW: ������), PULSE lag DIR(CW:������)

        [DllImport(LibraryFileName)]
        private static extern uint AxmMotSetPulseOutMethod(int nAxisNo, uint uMethod);
        // ���� ���� �޽� ��� ��� ������ ��ȯ�Ѵ�,
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotGetPulseOutMethod(int nAxisNo, ref uint upMethod);

        // ���� ���� �ܺ�(Actual) ī��Ʈ�� ���� ���� ������ �����Ͽ� ���� ���� Encoder �Է� ����� �����Ѵ�.
        // uMethod : 0 - 7 ����
        // ObverseUpDownMode    = 0x0      // ������ Up/Down
        // ObverseSqr1Mode      = 0x1      // ������ 1ü��
        // ObverseSqr2Mode      = 0x2      // ������ 2ü��
        // ObverseSqr4Mode      = 0x3      // ������ 4ü��
        // ReverseUpDownMode    = 0x4      // ������ Up/Down
        // ReverseSqr1Mode      = 0x5      // ������ 1ü��
        // ReverseSqr2Mode      = 0x6      // ������ 2ü��
        // ReverseSqr4Mode      = 0x7      // ������ 4ü��
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotSetEncInputMethod(int nAxisNo, uint uMethod);
        // ���� ���� �ܺ�(Actual) ī��Ʈ�� ���� ���� ������ �����Ͽ� ���� ���� Encoder �Է� ����� ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotGetEncInputMethod(int nAxisNo, ref uint upMethod);

        // ���� �ӵ� ������ RPM(Revolution Per Minute)���� ���߰� �ʹٸ�.
        // ex>    rpm ���:
        // 4500 rpm ?
        // unit/ pulse = 1 : 1�̸�      pulse/ sec �ʴ� �޽����� �Ǵµ�
        // 4500 rpm�� ���߰� �ʹٸ�     4500 / 60 �� : 75ȸ��/ 1��
        // ���Ͱ� 1ȸ���� �� �޽����� �˾ƾ� �ȴ�. �̰��� Encoder�� Z���� �˻��غ��� �˼��ִ�.
        // 1ȸ��:1800 �޽���� 75 x 1800 = 135000 �޽��� �ʿ��ϰ� �ȴ�.
        // AxmMotSetMoveUnitPerPulse�� Unit = 1, Pulse = 1800 �־� ���۽�Ų��.
        // �������� : rpm���� �����ϰ� �ȴٸ� �ӵ��� ���ӵ� �� rpm������ �ٲ�� �ȴ�.

        // ���� ���� �޽� �� �����̴� �Ÿ��� �����Ѵ�.
        [DllImport(LibraryFileName)]
        public static extern uint AxmMotSetMoveUnitPerPulse(int nAxisNo, double dUnit, int nPulse);
        // ���� ���� �޽� �� �����̴� �Ÿ��� ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotGetMoveUnitPerPulse(int nAxisNo, ref double dpUnit, ref int npPulse);

        // ���� �࿡ ���� ���� ����Ʈ ���� ����� �����Ѵ�.
        // uMethod : 0 -1 ����
        // AutoDetect = 0x0 : �ڵ� ������.
        // RestPulse  = 0x1 : ���� ������."
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotSetDecelMode(int nAxisNo, uint uMethod);
        // ���� ���� ���� ���� ����Ʈ ���� ����� ��ȯ�Ѵ�    
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotGetDecelMode(int nAxisNo, ref uint upMethod);

        // ���� �࿡ ���� ���� ��忡�� �ܷ� �޽��� �����Ѵ�.
        // �����: ���� AxmMotSetRemainPulse�� 500 �޽��� ����
        //           AxmMoveStartPos�� ��ġ 10000�� ��������쿡 9500�޽����� 
        //           ���� �޽� 500��  AxmMotSetMinVel�� ������ �ӵ��� �����ϸ鼭 ���� �ȴ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotSetRemainPulse(int nAxisNo, uint uData);
        // ���� ���� ���� ���� ��忡�� �ܷ� �޽��� ��ȯ�Ѵ�.    
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotGetRemainPulse(int nAxisNo, ref uint upData);

        // ���� �࿡ ��ӵ� ���� �Լ������� �ְ� �ӵ��� �����Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotSetMaxVel(int nAxisNo, double dVel);
        // ���� ���� ��ӵ� ���� �Լ������� �ְ� �ӵ��� ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotGetMaxVel(int nAxisNo, ref double dpVel);

        // ���� ���� �̵� �Ÿ� ��� ��带 �����Ѵ�.
        // uAbsRelMode  : POS_ABS_MODE '0' - ���� ��ǥ��
        //                POS_REL_MODE '1' - ��� ��ǥ��
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotSetAbsRelMode(int nAxisNo, uint uAbsRelMode);
        // ���� ���� ������ �̵� �Ÿ� ��� ��带 ��ȯ�Ѵ�
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotGetAbsRelMode(int nAxisNo, ref uint upAbsRelMode);

        // ���� ���� ���� �ӵ� �������� ��带 �����Ѵ�.
        // ProfileMode : SYM_TRAPEZOIDE_MODE    '0' - ��Ī Trapezode
        //               ASYM_TRAPEZOIDE_MODE   '1' - ���Ī Trapezode
        //               QUASI_S_CURVE_MODE     '2' - ��������
        //               SYM_S_CURVE_MODE       '3' - ��Ī S Curve
        //               ASYM_S_CURVE_MODE      '4' - ���Ī S Curve
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotSetProfileMode(int nAxisNo, uint uProfileMode);
        // ���� ���� ������ ���� �ӵ� �������� ��带 ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotGetProfileMode(int nAxisNo, ref uint upProfileMode);

        [Serializable]
        public enum AccelUnit : uint
        {
            UnitPerSec2 = 0,
            Second = 1
        }

        // ���� ���� ���ӵ� ������ �����Ѵ�.
        // AccelUnit : UNIT_SEC2   '0' - ������ ������ unit/sec2 ���
        //             SEC         '1' - ������ ������ sec ���
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotSetAccelUnit(int nAxisNo, uint uAccelUnit);
        // ���� ���� ������ ���ӵ������� ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotGetAccelUnit(int nAxisNo, ref uint upAccelUnit);

        // ���� �࿡ �ʱ� �ӵ��� �����Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotSetMinVel(int nAxisNo, double dMinVelocity);
        // ���� ���� �ʱ� �ӵ��� ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotGetMinVel(int nAxisNo, ref double dpMinVelocity);

        // ���� ���� ���� ��ũ���� �����Ѵ�.[%].
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotSetAccelJerk(int nAxisNo, double dAccelJerk);
        // ���� ���� ������ ���� ��ũ���� ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotGetAccelJerk(int nAxisNo, ref double dpAccelJerk);

        // ���� ���� ���� ��ũ���� �����Ѵ�.[%].
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotSetDecelJerk(int nAxisNo, double dDecelJerk);
        // ���� ���� ������ ���� ��ũ���� ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMotGetDecelJerk(int nAxisNo, ref double dpDecelJerk);

        #endregion

        #region ����� ��ȣ ���� �����Լ�

        // ���� ���� Z �� Level�� �����Ѵ�.
        // uLevel : LOW(0), HIGH(1)
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalSetZphaseLevel(int nAxisNo, uint uLevel);
        // ���� ���� Z �� Level�� ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalGetZphaseLevel(int nAxisNo, ref uint upLevel);

        // ���� ���� Servo-On��ȣ�� ��� ������ �����Ѵ�.
        // uLevel : LOW(0), HIGH(1)
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalSetServoOnLevel(int nAxisNo, uint uLevel);
        // ���� ���� Servo-On��ȣ�� ��� ���� ������ ��ȯ�Ѵ�.    
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalGetServoOnLevel(int nAxisNo, ref uint upLevel);

        // ���� ���� Servo-Alarm Reset ��ȣ�� ��� ������ �����Ѵ�.
        // uLevel : LOW(0), HIGH(1)
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalSetServoAlarmResetLevel(int nAxisNo, uint uLevel);
        // ���� ���� Servo-Alarm Reset ��ȣ�� ��� ������ ������ ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalGetServoAlarmResetLevel(int nAxisNo, ref uint upLevel);

        // ���� ���� Inpositon ��ȣ ��� ���� �� ��ȣ �Է� ������ �����Ѵ�
        // uLevel : LOW(0), HIGH(1), UNUSED(2), USED(3)    
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalSetInpos(int nAxisNo, uint uUse);
        // ���� ���� Inpositon ��ȣ ��� ���� �� ��ȣ �Է� ������ ��ȯ�Ѵ�.    
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalGetInpos(int nAxisNo, ref uint upUse);
        // ���� ���� Inpositon ��ȣ �Է� ���¸� ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalReadInpos(int nAxisNo, ref uint upStatus);

        // ���� ���� �˶� ��ȣ �Է� �� ��� ������ ��� ���� �� ��ȣ �Է� ������ �����Ѵ�.
        // uLevel : LOW(0), HIGH(1), UNUSED(2), USED(3)
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalSetServoAlarm(int nAxisNo, uint uUse);
        // ���� ���� �˶� ��ȣ �Է� �� ��� ������ ��� ���� �� ��ȣ �Է� ������ ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalGetServoAlarm(int nAxisNo, ref uint upUse);
        // ���� ���� �˶� ��ȣ�� �Է� ������ ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalReadServoAlarm(int nAxisNo, ref uint upStatus);

        // ���� ���� end limit sensor�� ��� ���� �� ��ȣ�� �Է� ������ �����Ѵ�. 
        // end limit sensor ��ȣ �Է� �� �������� �Ǵ� �������� ���� ������ �����ϴ�.
        // uStopMode: EMERGENCY_STOP(0), SLOWDOWN_STOP(1)
        // uPositiveLevel, uNegativeLevel : LOW(0), HIGH(1), UNUSED(2), USED(3)
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalSetLimit(int nAxisNo, uint uStopMode, uint uPositiveLevel, uint uNegativeLevel);
        // ���� ���� end limit sensor�� ��� ���� �� ��ȣ�� �Է� ����, ��ȣ �Է� �� ������带 ��ȯ�Ѵ�
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalGetLimit(int nAxisNo, ref uint upStopMode, ref uint upPositiveLevel, ref uint upNegativeLevel);
        // �������� end limit sensor�� �Է� ���¸� ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalReadLimit(int nAxisNo, ref uint upPositiveStatus, ref uint upNegativeStatus);

        // ���� ���� Software limit�� ��� ����, ����� ī��Ʈ, �׸��� ���� ����� �����Ѵ�
        // uUse       : DISABLE(0), ENABLE(1)
        // uStopMode  : EMERGENCY_STOP(0), SLOWDOWN_STOP(1)
        // uSelection : COMMAND(0), ACTUAL(1)
        // ���ǻ���: �����˻��� ���Լ��� �̿��Ͽ� ����Ʈ���� ������ �̸� �����ؼ� ������ �����˻��� �����˻��� ���߿� ���߾�������쿡��  Enable�ȴ�. 
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalSetSoftLimit(int nAxisNo, uint uUse, uint uStopMode, uint uSelection, double dPositivePos, double dNegativePos);
        // ���� ���� Software limit�� ��� ����, ����� ī��Ʈ, �׸��� ���� ����� ��ȯ�Ѵ�
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalGetSoftLimit(int nAxisNo, ref uint upUse, ref uint upStopMode, ref uint upSelection, ref double dpPositivePos, ref double dpNegativePos);

        // ��� ���� ��ȣ�� ���� ��� (������/��������) �Ǵ� ��� ������ �����Ѵ�.
        // uStopMode  : EMERGENCY_STOP(0), SLOWDOWN_STOP(1)
        // uLevel : LOW(0), HIGH(1), UNUSED(2), USED(3)
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalSetStop(int nAxisNo, uint uStopMode, uint uLevel);
        // ��� ���� ��ȣ�� ���� ��� (������/��������) �Ǵ� ��� ������ ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalGetStop(int nAxisNo, ref uint upStopMode, ref uint upLevel);
        // ��� ���� ��ȣ�� �Է� ���¸� ��ȯ�Ѵ�.    
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalReadStop(int nAxisNo, ref uint upStatus);

        // ���� ���� Servo-On ��ȣ�� ����Ѵ�.
        // uOnOff : FALSE(0), TRUE(1) ( ���� 0��¿� �ش��)
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalServoOn(int nAxisNo, uint uUse);
        // ���� ���� Servo-On ��ȣ�� ��� ���¸� ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalIsServoOn(int nAxisNo, ref uint upUse);

        // ���� ���� Servo-Alarm Reset ��ȣ�� ����Ѵ�.
        // uOnOff : FALSE(0), TRUE(1) ( ���� 1��¿� �ش��)
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalServoAlarmReset(int nAxisNo, uint nOnOff);

        // ���� ��°��� �����Ѵ�.
        // uValue : Hex Value 0x00
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalWriteOutput(int nAxisNo, uint uValue);
        // ���� ��°��� ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalReadOutput(int nAxisNo, ref uint upValue);

        // lBitNo : Bit Number(0 - 4)
        // uOnOff : FALSE(0), TRUE(1)
        // ���� ��°��� ��Ʈ���� �����Ѵ�.
        [DllImport(LibraryFileName)]
        public static extern uint AxmSignalWriteOutputBit(int nAxisNo, int nBitNo, uint uOn);
        // ���� ��°��� ��Ʈ���� ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalReadOutputBit(int nAxisNo, int nBitNo, ref uint upOn);

        // ���� �Է°��� Hex������ ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalReadInput(int nAxisNo, ref uint upValue);

        // lBitNo : Bit Number(0 - 4)
        // ���� �Է°��� ��Ʈ���� ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmSignalReadInputBit(int nAxisNo, int nBitNo, ref uint upOn);

        #endregion

        #region ��� ������ �� �����Ŀ� ���� Ȯ���ϴ� �Լ�

        // ���� ���� �޽� ��� ���¸� ��ȯ�Ѵ�.
        // (��������)"
        [DllImport(LibraryFileName)]
        private static extern uint AxmStatusReadInMotion(int nAxisNo, ref uint upStatus);

        // �������� ���� ���� ���� ���� �޽� ī���� ���� ��ȯ�Ѵ�.
        // ���ǻ���: �����߿��� ī���Ͱ��� ǥ���ϰ� ���������Ŀ��� ī���Ͱ��� CLEAR�ȴ�.    
        //  (�޽� ī��Ʈ ��)"
        [DllImport(LibraryFileName)]
        private static extern uint AxmStatusReadDrivePulseCount(int nAxisNo, ref int npPulse);

        // DriveStatus �������͸� Ȯ��
        [DllImport(LibraryFileName)]
        private static extern uint AxmStatusReadMotion(int nAxisNo, ref uint upStatus);



        // ���� ���� ��� ����(Cmd, Act, Driver Status, Mechanical Signal, Universal Signal)�� �ѹ��� Ȯ�� �� �� �ִ�.
        // MOTION_INFO ����ü�� uMask �������� ��� ���� ������ �����Ѵ�.
        // uMask : ��� ���� ǥ��(6bit) - ex) uMask = 0x1F ���� �� ��� ���¸� ǥ����.
        // ����ڰ� ������ Level(In/Out)�� �ݿ����� ����.
        //    [0]        |    Command Position Read
        //    [1]        |    Actual Position Read
        //    [2]        |    Mechanical Signal Read
        //    [3]        |    Driver Status Read
        //    [4]        |    Universal Signal Input Read
        //               |    Universal Signal Output Read
        [DllImport(LibraryFileName)]
        private static extern uint AxmStatusReadMotionInfo(int nAxisNo, ref MOTION_INFO MI);


        // EndStatus �������͸� Ȯ��
        [DllImport(LibraryFileName)]
        private static extern uint AxmStatusReadStop(int nAxisNo, ref uint upStatus);

        // ���� ���� Mechanical Signal Data(���� ������� ��ȣ����) �� ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmStatusReadMechanical(int nAxisNo, ref uint upStatus);

        // ���� ���� ���� ���� �ӵ��� �о�´�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmStatusReadVel(int nAxisNo, ref double dpVelocity);

        // Command Pos�� Actual Pos�� ���� Ȯ��
        [DllImport(LibraryFileName)]
        private static extern uint AxmStatusReadPosError(int nAxisNo, ref double dpError);

        // ���� ����̺��� �̵� �Ÿ��� Ȯ��
        [DllImport(LibraryFileName)]
        private static extern uint AxmStatusReadDriveDistance(int nAxisNo, ref double dpUnit);

        // ���� ���� Actual ��ġ�� �����Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmStatusSetActPos(int nAxisNo, double dPos);
        // ���� ���� Actual ��ġ�� ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmStatusGetActPos(int nAxisNo, ref double dpPos);

        // ���� ���� Command ��ġ�� �����Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmStatusSetCmdPos(int nAxisNo, double dPos);
        // ���� ���� Command ��ġ�� ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmStatusGetCmdPos(int nAxisNo, ref double dpPos);
        // ���� ���� Torque �� ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmStatusReadTorque(int nAxisNo, ref double dpTorque);



        #endregion

        #region Ȩ���� �Լ�

        // ���� ���� Home ���� Level �� �����Ѵ�.
        // uLevel : LOW(0), HIGH(1)
        [DllImport(LibraryFileName)]
        private static extern uint AxmHomeSetSignalLevel(int nAxisNo, uint uLevel);
        // ���� ���� Home ���� Level �� ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmHomeGetSignalLevel(int nAxisNo, ref uint upLevel);
        // ���� Ȩ ��ȣ �Է»��¸� Ȯ���Ѵ�. Ȩ��ȣ�� ����ڰ� ���Ƿ� AxmHomeSetMethod �Լ��� �̿��Ͽ� �����Ҽ��ִ�.
        // upStatus : OFF(0), ON(1)
        [DllImport(LibraryFileName)]
        private static extern uint AxmHomeReadSignal(int nAxisNo, ref uint upStatus);

        // �ش� ���� �����˻��� �����ϱ� ���ؼ��� �ݵ�� ���� �˻����� �Ķ��Ÿ���� �����Ǿ� �־�� �˴ϴ�. 
        // ���� MotionPara���� ������ �̿��� �ʱ�ȭ�� ���������� ����ƴٸ� ������ ������ �ʿ����� �ʴ�. 
        // �����˻� ��� �������� �˻� �������, �������� ����� ��ȣ, �������� Active Level, ���ڴ� Z�� ���� ���� ���� ���� �Ѵ�.
        // (�ڼ��� ������ AxmMotSaveParaAll ���� �κ� ����)
        // Ȩ������ AxmSignalSetHomeLevel ����Ѵ�.
        // HClrTim : HomeClear Time : ���� �˻� Encoder �� Set�ϱ� ���� ���ð� 
        // HmDir(Ȩ ����): DIR_CCW (0) -���� , DIR_CW(1) +����
        // HOffset - ���������� �̵��Ÿ�.
        // uZphas: 1�� �����˻� �Ϸ� �� ���ڴ� Z�� ���� ���� ����  0: ������ , 1: +����, 2: -���� 
        // HmSig : PosEndLimit(0) -> +Limit
        //         NegEndLimit(1) -> -Limit
        //         HomeSensor (4) -> ��������(���� �Է� 0)
        [DllImport(LibraryFileName)]
        private static extern uint AxmHomeSetMethod(int nAxisNo, int nHmDir, uint uHomeSignal, uint uZphas, double dHomeClrTime, double dHomeOffset);
        // �����Ǿ��ִ� Ȩ ���� �Ķ��Ÿ���� ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmHomeGetMethod(int nAxisNo, ref int nHmDir, ref uint uHomeSignal, ref uint uZphas, ref double dHomeClrTime, ref double dHomeOffset);

        // ������ ������ �����ϰ� �˻��ϱ� ���� ���� �ܰ��� �������� �����Ѵ�. �̶� �� ���ǿ� ��� �� �ӵ��� �����Ѵ�. 
        // �� �ӵ����� �������� ���� �����˻� �ð���, �����˻� ���е��� �����ȴ�. 
        // �� ���Ǻ� �ӵ����� ������ �ٲ㰡�鼭 �� ���� �����˻� �ӵ��� �����ϸ� �ȴ�. 
        // (�ڼ��� ������ AxmMotSaveParaAll ���� �κ� ����)
        // �����˻��� ���� �ӵ��� �����ϴ� �Լ�
        // [dVelFirst]- 1�������ӵ�   [dVelSecond]-�����ļӵ�   [dVelThird]- ������ �ӵ�  [dvelLast]- index�˻��� �����ϰ� �˻��ϱ�����. 
        // [dAccFirst]- 1���������ӵ� [dAccSecond]-�����İ��ӵ� 
        [DllImport(LibraryFileName)]
        private static extern uint AxmHomeSetVel(int nAxisNo, double dVelFirst, double dVelSecond, double dVelThird, double dvelLast, double dAccFirst, double dAccSecond);
        // �����Ǿ��ִ� �����˻��� ���� �ӵ��� ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmHomeGetVel(int nAxisNo, ref double dVelFirst, ref double dVelSecond, ref double dVelThird, ref double dvelLast, ref double dAccFirst, ref double dAccSecond);

        // �����˻��� �����Ѵ�.
        // �����˻� �����Լ��� �����ϸ� ���̺귯�� ���ο��� �ش����� �����˻��� ���� �� �����尡 �ڵ� �����Ǿ� �����˻��� ���������� ������ �� �ڵ� ����ȴ�.
        // ���ǻ��� : �������� �ݴ������ ����Ʈ ������ ���͵� ��������� ������ ACTIVE���������� �����Ѵ�.
        //            ���� �˻��� ���۵Ǿ� ��������� ����Ʈ ������ ������ ����Ʈ ������ �����Ǿ��ٰ� �����ϰ� �����ܰ�� ����ȴ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmHomeSetStart(int nAxisNo);

        // �����˻� ����� ����ڰ� ���Ƿ� �����Ѵ�.
        // �����˻� �Լ��� �̿��� ���������� �����˻��� ����ǰ���� �˻� ����� HOME_SUCCESS�� �����˴ϴ�.
        // �� �Լ��� ����ڰ� �����˻��� ���������ʰ� ����� ���Ƿ� ������ �� �ִ�. 
        // uHomeResult ����
        // HOME_SUCCESS              = 0x01      // Ȩ �Ϸ�
        // HOME_SEARCHING            = 0x02      // Ȩ�˻���
        // HOME_ERR_GNT_RANGE        = 0x10      // Ȩ �˻� ������ ��������
        // HOME_ERR_USER_BREAK       = 0x11      // �ӵ� ������ ���Ƿ� ��������� ���������
        // HOME_ERR_VELOCITY         = 0x12      // �ӵ� ���� �߸��������
        // HOME_ERR_AMP_FAULT        = 0x13      // ������ �˶� �߻� ����
        // HOME_ERR_NEG_LIMIT        = 0x14      // (-)���� ������ (+)����Ʈ ���� ���� ����
        // HOME_ERR_POS_LIMIT        = 0x15      // (+)���� ������ (-)����Ʈ ���� ���� ����
        // HOME_ERR_NOT_DETECT       = 0x16      // ������ ��ȣ �������� �� �� ��� ����
        // HOME_ERR_UNKNOWN          = 0xFF    
        [DllImport(LibraryFileName)]
        private static extern uint AxmHomeSetResult(int nAxisNo, uint uHomeResult);
        // �����˻� ����� ��ȯ�Ѵ�.
        // �����˻� �Լ��� �˻� ����� Ȯ���Ѵ�. �����˻��� ���۵Ǹ� HOME_SEARCHING���� �����Ǹ� �����˻��� �����ϸ� ���п����� �����ȴ�. ���� ������ ������ �� �ٽ� �����˻��� �����ϸ� �ȴ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmHomeGetResult(int nAxisNo, ref uint upHomeResult);
        // �����˻� ������� ��ȯ�Ѵ�.
        // �����˻� ���۵Ǹ� �������� Ȯ���� �� �ִ�. �����˻��� �Ϸ�Ǹ� �������ο� ������� 100�� ��ȯ�ϰ� �ȴ�. �����˻� �������δ� GetHome Result�Լ��� �̿��� Ȯ���� �� �ִ�.
        // upHomeMainStepNumber : Main Step �������̴�. 
        // ��Ʈ�� FALSE�� ���upHomeMainStepNumber : 0 �϶��� ������ �ุ ��������̰� Ȩ �������� upHomeStepNumber ǥ���Ѵ�.
        // ��Ʈ�� TRUE�� ��� upHomeMainStepNumber : 0 �϶��� ������ Ȩ�� ��������̰� ������ Ȩ �������� upHomeStepNumber ǥ���Ѵ�.
        // ��Ʈ�� TRUE�� ��� upHomeMainStepNumber : 10 �϶��� �����̺� Ȩ�� ��������̰� ������ Ȩ �������� upHomeStepNumber ǥ���Ѵ�.
        // upHomeStepNumber     : ������ �࿡���� �������� ǥ���Ѵ�. 
        // ��Ʈ�� FALSE�� ���  : ������ �ุ �������� ǥ���Ѵ�.
        // ��Ʈ�� TRUE�� ��� ��������, �����̺��� ������ �������� ǥ�õȴ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmHomeGetRate(int nAxisNo, ref uint upHomeMainStepNumber, ref uint upHomeStepNumber);

        #endregion

        #region ��ġ �����Լ�

        // ���ǻ���: ��ġ�� �����Ұ�� �ݵ�� UNIT/PULSE�� ���߾ �����Ѵ�.
        //           ��ġ�� UNIT/PULSE ���� �۰��� ��� �ּҴ����� UNIT/PULSE�� ���߾����⶧���� ����ġ���� ������ �ɼ�����.
        // ���� �ӵ� ������ RPM(Revolution Per Minute)���� ���߰� �ʹٸ�.
        // ex>    rpm ���:
        // 4500 rpm ?
        // unit/ pulse = 1 : 1�̸�      pulse/ sec �ʴ� �޽����� �Ǵµ�
        // 4500 rpm�� ���߰� �ʹٸ�     4500 / 60 �� : 75ȸ��/ 1��
        // ���Ͱ� 1ȸ���� �� �޽����� �˾ƾ� �ȴ�. �̰��� Encoder�� Z���� �˻��غ��� �˼��ִ�.
        // 1ȸ��:1800 �޽���� 75 x 1800 = 135000 �޽��� �ʿ��ϰ� �ȴ�.
        // AxmMotSetMoveUnitPerPulse�� Unit = 1, Pulse = 1800 �־� ���۽�Ų��. 

        // ������ �Ÿ���ŭ �Ǵ� ��ġ���� �̵��Ѵ�.
        // ���� ���� ���� ��ǥ/ �����ǥ �� ������ ��ġ���� ������ �ӵ��� �������� ������ �Ѵ�.
        // �ӵ� ���������� AxmMotSetProfileMode �Լ����� �����Ѵ�.
        // �޽��� ��µǴ� �������� �Լ��� �����.
        // Vel���� ����̸� CW, �����̸� CCW �������� ����.
        // AxmMotSetAccelUnit(lAxisNo, 1) �ϰ�� dAccel -> dAccelTime , dDecel -> dDecelTime ���� �ٲ��.
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

        // ������ �Ÿ���ŭ �Ǵ� ��ġ���� �̵��Ѵ�.
        // ���� ���� ���� ��ǥ/�����ǥ�� ������ ��ġ���� ������ �ӵ��� �������� ������ �Ѵ�.
        // �ӵ� ���������� AxmMotSetProfileMode �Լ����� �����Ѵ�. 
        // �޽� ����� ����Ǵ� �������� �Լ��� �����
        // Vel���� ����̸� CW, �����̸� CCW �������� ����.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMovePos(int nAxisNo, double dPos, double dVel, double dAccel, double dDecel);

        // ������ �ӵ��� �����Ѵ�.
        // ���� �࿡ ���Ͽ� ������ �ӵ��� �������� ���������� �ӵ� ��� ������ �Ѵ�. 
        // �޽� ����� ���۵Ǵ� �������� �Լ��� �����.
        // Vel���� ����̸� CW, �����̸� CCW �������� ����.
        [DllImport(LibraryFileName)]
        public static extern uint AxmMoveVel(int nAxisNo, double dVel, double dAccel, double dDecel);

        // ������ ���࿡ ���Ͽ� ������ �ӵ��� �������� ���������� �ӵ� ��� ������ �Ѵ�.
        // �޽� ����� ���۵Ǵ� �������� �Լ��� �����.
        // Vel���� ����̸� CW, �����̸� CCW �������� ����.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMoveStartMultiVel(int lArraySize, ref int lpAxesNo, ref double dVel, ref double dAccel, ref double dDecel);

        // Ư�� Input ��ȣ�� Edge�� �����Ͽ� ������ �Ǵ� ���������ϴ� �Լ�.
        // lDetect Signal : edge ������ �Է� ��ȣ ����.
        // lDetectSignal  : PosEndLimit(0), NegEndLimit(1), HomeSensor(4), EncodZPhase(5), UniInput02(6), UniInput03(7)
        // Signal Edge    : ������ �Է� ��ȣ�� edge ���� ���� (rising or falling edge).
        //                    SIGNAL_DOWN_EDGE(0), SIGNAL_UP_EDGE(1)
        // ��������       : Vel���� ����̸� CW, �����̸� CCW.
        // SignalMethod   : ������ EMERGENCY_STOP(0), �������� SLOWDOWN_STOP(1)
        // ���ǻ��� : SignalMethod�� EMERGENCY_STOP(0)�� ����Ұ�� �������� ���õǸ� ������ �ӵ��� ���� �������ϰԵȴ�.
        //            PCI-Nx04�� ����� ��� lDetectSignal�� PosEndLimit , NegEndLimit(0,1) �� ã����� ��ȣ�Ƿ��� Active ���¸� �����ϰԵȴ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMoveSignalSearch(int nAxisNo, double dVel, double dAccel, int nDetectSignal, int nSignalEdge, int nSignalMethod);

        // ���� �࿡�� ������ ��ȣ�� �����ϰ� �� ��ġ�� �����ϱ� ���� �̵��ϴ� �Լ��̴�.
        // ���ϴ� ��ȣ�� ��� ã�� �����̴� �Լ� ã�� ��� �� ��ġ�� ������ѳ��� AxmGetCapturePos����Ͽ� �װ��� �д´�.
        // Signal Edge   : ������ �Է� ��ȣ�� edge ���� ���� (rising or falling edge).
        //                 SIGNAL_DOWN_EDGE(0), SIGNAL_UP_EDGE(1)
        // ��������      : Vel���� ����̸� CW, �����̸� CCW.
        // SignalMethod  : ������ EMERGENCY_STOP(0), �������� SLOWDOWN_STOP(1)
        // lDetect Signal: edge ������ �Է� ��ȣ ����.SIGNAL_DOWN_EDGE(0), SIGNAL_UP_EDGE(1)
        // lDetectSignal : PosEndLimit(0), NegEndLimit(1), HomeSensor(4), EncodZPhase(5), UniInput02(6), UniInput03(7)
        // lTarget       : COMMAND(0), ACTUAL(1)
        // ���ǻ���: SignalMethod�� EMERGENCY_STOP(0)�� ����Ұ�� �������� ���õǸ� ������ �ӵ��� ���� �������ϰԵȴ�.
        //           lDetectSignal�� PosEndLimit , NegEndLimit(0,1) �� ã����� ��ȣ�Ƿ��� Active ���¸� �����ϰԵȴ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMoveSignalCapture(int nAxisNo, double dVel, double dAccel, int nDetectSignal, int nSignalEdge, int nTarget, int nSignalMethod);

        // 'AxmMoveSignalCapture' �Լ����� ����� ��ġ���� Ȯ���ϴ� �Լ��̴�.
        // ���ǻ���: �Լ� ���� ����� "AXT_RT_SUCCESS"�϶� ����� ��ġ�� ��ȿ�ϸ�, �� �Լ��� �ѹ� �����ϸ� ���� ��ġ���� �ʱ�ȭ�ȴ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMoveGetCapturePos(int nAxisNo, ref double dpCapPos);

        // "������ �Ÿ���ŭ �Ǵ� ��ġ���� �̵��ϴ� �Լ�.
        // �Լ��� �����ϸ� �ش� Motion ������ ������ �� Motion �� �Ϸ�ɶ����� ��ٸ��� �ʰ� �ٷ� �Լ��� ����������."
        [DllImport(LibraryFileName)]
        private static extern uint AxmMoveStartMultiPos(int nArraySize, ref int nAxisNo, ref double dPos, ref double dVel, ref double dAccel, ref double dDecel);

        // ������ ������ �Ÿ���ŭ �Ǵ� ��ġ���� �̵��Ѵ�.
        // ���� ����� ���� ��ǥ�� ������ ��ġ���� ������ �ӵ��� �������� ������ �Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMoveMultiPos(int nArraySize, ref int nAxisNo, ref double dPos, ref double dVel, ref double dAccel, ref double dDecel);

        // ���� ���� ������ ���ӵ��� ���� ���� �Ѵ�.
        // dDecel : ���� �� ��������
        [DllImport(LibraryFileName)]
        private static extern uint AxmMoveStop(int nAxisNo, double dDecel);
        // ���� ���� �� ���� �Ѵ�.
        [DllImport(LibraryFileName)]
        public static extern uint AxmMoveEStop(int nAxisNo);
        // ���� ���� ���� �����Ѵ�.
        [DllImport(LibraryFileName)]
        public static extern uint AxmMoveSStop(int nAxisNo);

        #endregion

        #region �������̵� �Լ�

        // ��ġ �������̵� �Ѵ�.
        // ���� ���� ������ ����Ǳ� �� ������ ��� �޽� ���� �����Ѵ�.
        // PCI-Nx04 �������ǻ���: �������̵��� ��ġ�� �������� ���� ������ ��ġ�� ���������� Relative ������ ��ġ������ �־��ش�.
        //                          ���������� ���������� ��� �������̵带 ����Ҽ������� �ݴ�������� �������̵��Ұ�쿡�� �������̵带 ����Ҽ�����.
        [DllImport(LibraryFileName)]
        private static extern uint AxmOverridePos(int nAxisNo, double dOverridePos);

        // ���� ���� �ӵ��������̵� �ϱ����� �������̵��� �ְ�ӵ��� �����Ѵ�.
        // ������ : �ӵ��������̵带 5���Ѵٸ� ���߿� �ְ� �ӵ��� �����ؾߵȴ�. 
        [DllImport(LibraryFileName)]
        private static extern uint AxmOverrideSetMaxVel(int nAxisNo, double dOverrideMaxVel);

        // �ӵ� �������̵� �Ѵ�.
        // ���� ���� ���� �߿� �ӵ��� ���� �����Ѵ�. (�ݵ�� ��� �߿� ���� �����Ѵ�.)
        // ������: AxmOverrideVel �Լ��� ����ϱ�����. AxmOverrideMaxVel �ְ�� �����Ҽ��ִ� �ӵ��� �����س��´�.
        // EX> �ӵ��������̵带 �ι��Ѵٸ� 
        // 1. �ΰ��߿� ���� �ӵ��� AxmOverrideMaxVel ���� �ְ� �ӵ��� ����.
        // 2. AxmMoveStartPos ���� ���� ���� ���� ��(Move�Լ� ��� ����)�� �ӵ��� ù��° �ӵ��� AxmOverrideVel ���� �����Ѵ�.
        // 3. ���� ���� ���� ��(Move�Լ� ��� ����)�� �ӵ��� �ι�° �ӵ��� AxmOverrideVel ���� �����Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmOverrideVel(int nAxisNo, double dOverrideVelocity);

        // ���ӵ�, �ӵ�, ���ӵ���  �������̵� �Ѵ�.
        // ���� ���� ���� �߿� ���ӵ�, �ӵ�, ���ӵ��� ���� �����Ѵ�. (�ݵ�� ��� �߿� ���� �����Ѵ�.)
        // ������: AxmOverrideAccelVelDecel �Լ��� ����ϱ�����. AxmOverrideMaxVel �ְ�� �����Ҽ��ִ� �ӵ��� �����س��´�.
        // EX> �ӵ��������̵带 �ι��Ѵٸ� 
        // 1. �ΰ��߿� ���� �ӵ��� AxmOverrideMaxVel ���� �ְ� �ӵ��� ����.
        // 2. AxmMoveStartPos ���� ���� ���� ���� ��(Move�Լ� ��� ����)�� ���ӵ�, �ӵ�, ���ӵ��� ù��° �ӵ��� AxmOverrideAccelVelDecel ���� �����Ѵ�.
        // 3. ���� ���� ���� ��(Move�Լ� ��� ����)�� ���ӵ�, �ӵ�, ���ӵ��� �ι�° �ӵ��� AxmOverrideAccelVelDecel ���� �����Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmOverrideAccelVelDecel(int nAxisNo, double dOverrideVelocity, double dMaxAccel, double dMaxDecel);

        // ��� �������� �ӵ� �������̵� �Ѵ�.
        // ��� ��ġ ������ �������̵��� �ӵ��� �Է½��� ����ġ���� �ӵ��������̵� �Ǵ� �Լ�
        // lTarget : COMMAND(0), ACTUAL(1)
        // ������: AxmOverrideVelAtPos �Լ��� ����ϱ�����. AxmOverrideMaxVel �ְ�� �����Ҽ��ִ� �ӵ��� �����س��´�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmOverrideVelAtPos(int nAxisNo, double dPos, double dVel, double dAccel, double dDecel, double dOverridePos, double dOverrideVelocity, int nTarget);

        #endregion

        #region ������, �����̺�  ����� ���� �Լ�

        // Electric Gear ��忡�� Master ��� Slave ����� ���� �����Ѵ�.
        // dSlaveRatio : �������࿡ ���� �����̺��� ����( 0 : 0% , 0.5 : 50%, 1 : 100%)
        [DllImport(LibraryFileName)]
        private static extern uint AxmLinkSetMode(int nMasterAxisNo, int nSlaveAxisNo, double dSlaveRatio);
        // Electric Gear ��忡�� ������ Master ��� Slave ����� ���� ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmLinkGetMode(int nMasterAxisNo, ref uint nSlaveAxisNo, ref double dpGearRatio);
        // Master ��� Slave�ణ�� ���ڱ��� ���� ���� �Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmLinkResetMode(int nMasterAxisNo);

        #endregion

        #region ��Ʈ�� ���� �Լ�
        [Serializable]
        public enum GantryHomingMethods
        {
            OnlyMaster = 0,
            MasterSlaveWithOffset = 1,
            MasterSlaveWithoutOffset = 2,
        }

        // ��Ǹ���� �� ���� �ⱸ������ Link�Ǿ��ִ� ��Ʈ�� �����ý��� ��� �����Ѵ�. 
        // �� �Լ��� �̿��� Master���� ��Ʈ�� ����� �����ϸ� �ش� Slave���� Master��� ����Ǿ� �����˴ϴ�. 
        // ���� ��Ʈ�� ���� ���� Slave�࿡ ��������̳� ���� ��ɵ��� ������ ��� ���õ˴ϴ�.
        // uSlHomeUse     : �������� Ȩ��� ��� (0 - 2)
        //             (0 : �����̺��� Ȩ�� �����ϰ� ���������� Ȩ�� ã�´�.)
        //             (1 : �������� , �����̺��� Ȩ�� ã�´�. �����̺� dSlOffset �� �����ؼ� ������.)
        //             (2 : �������� , �����̺��� Ȩ�� ã�´�. �����̺� dSlOffset �� �����ؼ� ��������.)
        // dSlOffset      : �����̺��� �ɼ°�
        // dSlOffsetRange : �����̺��� �ɼ°� ������ ����
        // PCI-Nx04 �������ǻ���: ��Ʈ�� ENABLE�� �����̺����� ����� AxmStatusReadMotion �Լ��� Ȯ���ϸ� True(Motion ���� ��)�� Ȯ�εǾ� �������̴�. 
        //                   �����̺��࿡ AxmStatusReadMotion�� Ȯ�������� InMotion �� False�̸� Gantry Enable�� �ȵȰ��̹Ƿ� �˶� Ȥ�� ����Ʈ ���� ���� Ȯ���Ѵ�.
        [DllImport(LibraryFileName)]
        public static extern uint AxmGantrySetEnable(int nMasterAxisNo, int nSlaveAxisNo, uint uSlHomeUse, double dSlOffset, double dSlOffsetRange);

        // Slave���� Offset���� �˾Ƴ��¹��.
        // A. ������, �����̺긦 �ΰ��� �������� ��Ų��.         
        // B. AxmGantrySetEnable�Լ����� uSlHomeUse = 2�� ������ AxmHomeSetStart�Լ��� �̿��ؼ� Ȩ�� ã�´�. 
        // C. Ȩ�� ã�� ���� ���������� Command���� �о�� ��������� �����̺����� Ʋ���� Offset���� �����ִ�.
        // D. Offset���� �о AxmGantrySetEnable�Լ��� dSlOffset���ڿ� �־��ش�. 
        // E. dSlOffset���� �־��ٶ� �������࿡ ���� �����̺� �� ���̱⶧���� ��ȣ�� �ݴ�� -dSlOffset �־��ش�.
        // F. dSIOffsetRange �� Slave Offset�� Range ������ ���ϴµ� Range�� �Ѱ踦 �����Ͽ� �Ѱ踦 ����� ������ �߻���ų�� ����Ѵ�.        
        // G. AxmGantrySetEnable�Լ��� Offset���� �־�������  AxmGantrySetEnable�Լ����� uSlHomeUse = 1�� ������ AxmHomeSetStart�Լ��� �̿��ؼ� Ȩ�� ã�´�.         

        // ��Ʈ�� ������ �־� ����ڰ� ������ �Ķ��Ÿ�� ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmGantryGetEnable(int nMasterAxisNo, ref uint upSlHomeUse, ref double dpSlOffset, ref double dSlORange, ref uint uGatryOn);

        // ��� ����� �� ���� �ⱸ������ Link�Ǿ��ִ� ��Ʈ�� �����ý��� ��� �����Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmGantrySetDisable(int nMasterAxisNo, int nSlaveAxisNo);

        #endregion

        #region �Ϲ� �����Լ�

        // ���ǻ���1: AxmContiSetAxisMap�Լ��� �̿��Ͽ� ������Ŀ� ������������� ������ �ϸ鼭 ����ؾߵȴ�.
        //           ��ȣ������ ��쿡�� �ݵ�� ������������� ��迭�� �־�� ���� �����ϴ�.

        // ���ǻ���2: ��ġ�� �����Ұ�� �ݵ�� ��������� �����̺� ���� UNIT/PULSE�� ���߾ �����Ѵ�.
        //           ��ġ�� UNIT/PULSE ���� �۰� ������ ��� �ּҴ����� UNIT/PULSE�� ���߾����⶧���� ����ġ���� ������ �ɼ�����.

        // ���ǻ���3: ��ȣ ������ �Ұ�� �ݵ�� ��Ĩ������ ������ �ɼ������Ƿ� 

        // ���ǻ���4: ���� ���� ����/�߿� ������ ���� ����(+- Limit��ȣ, ���� �˶�, ������� ��)�� �߻��ϸ� 
        //            ���� ���⿡ ������� ������ �������� �ʰų� ���� �ȴ�.

        // ���� ���� �Ѵ�.
        // �������� �������� �����Ͽ� ���� ���� ���� �����ϴ� �Լ��̴�. ���� ���� �� �Լ��� �����.
        // AxmContiBeginNode, AxmContiEndNode�� ���̻��� ������ ��ǥ�迡 �������� �������� �����Ͽ� ���� ���� �����ϴ� Queue�� �����Լ����ȴ�. 
        // ���� �������� ���� ���� ������ ���� ���� Queue�� �����Ͽ� AxmContiStart�Լ��� ����ؼ� �����Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmLineMove(int lCoord, ref double dPos, double dVel, double dAccel, double dDecel);

        // 2�� ��ȣ���� �Ѵ�.
        // ������, �������� �߽����� �����Ͽ� ��ȣ ���� �����ϴ� �Լ��̴�. ���� ���� �� �Լ��� �����.
        // AxmContiBeginNode, AxmContiEndNode, �� ���̻��� ������ ��ǥ�迡 ������, �������� �߽����� �����Ͽ� �����ϴ� ��ȣ ���� Queue�� �����Լ����ȴ�.
        // �������� ��ȣ ���� ���� ������ ���� ���� Queue�� �����Ͽ� AxmContiStart�Լ��� ����ؼ� �����Ѵ�.
        // dCenterPos = �߽��� X,Y  , dEndPos = ������ X,Y .
        // uCWDir   DIR_CCW(0): �ݽð����, DIR_CW(1) �ð����
        [DllImport(LibraryFileName)]
        private static extern uint AxmCircleCenterMove(int lCoord, ref int lAxisNo, ref double dCenterPos, ref double dEndPos, double dVel, double dAccel, double dDecel, uint uCWDir);

        // �߰���, �������� �����Ͽ� ��ȣ ���� �����ϴ� �Լ��̴�. ���� ���� �� �Լ��� �����.
        // AxmContiBeginNode, AxmContiEndNode�� ���̻��� ������ ��ǥ�迡 �߰���, �������� �����Ͽ� �����ϴ� ��ȣ ���� Queue�� �����Լ����ȴ�.
        // �������� ��ȣ ���� ���� ������ ���� ���� Queue�� �����Ͽ� AxmContiStart�Լ��� ����ؼ� �����Ѵ�.
        // dMidPos = �߰��� X,Y  , dEndPos = ������ X,Y 
        // uCWDir   DIR_CCW(0): �ݽð����, DIR_CW(1) �ð����
        [DllImport(LibraryFileName)]
        private static extern uint AxmCirclePointMove(int lCoord, ref int lAxisNo, ref double dMidPos, ref double dEndPos, double dVel, double dAccel, double dDecel);

        // ������, �������� �������� �����Ͽ� ��ȣ ���� �����ϴ� �Լ��̴�. ���� ���� �� �Լ��� �����.
        // AxmContiBeginNode, AxmContiEndNode�� ���̻��� ������ ��ǥ�迡 ������, �������� �������� �����Ͽ� ��ȣ ���� �����ϴ� Queue�� �����Լ����ȴ�.
        // �������� ��ȣ ���� ���� ������ ���� ���� Queue�� �����Ͽ� AxmContiStart�Լ��� ����ؼ� �����Ѵ�.
        // lAxisNo = ���� �迭 , dRadius = ������, dEndPos = ������ X,Y �迭 , uShortDistance = ������(0), ū��(1)
        // uCWDir   DIR_CCW(0): �ݽð����, DIR_CW(1) �ð����
        [DllImport(LibraryFileName)]
        private static extern uint AxmCircleRadiusMove(int lCoord, ref int lAxisNo, double dRadius, ref double dEndPos, double dVel, double dAccel, double dDecel, uint uCWDir, uint uShortDistance);

        // ������, ȸ�������� �������� �����Ͽ� ��ȣ ���� �����ϴ� �Լ��̴�. ���� ���� �� �Լ��� �����.
        // AxmContiBeginNode, AxmContiEndNode�� ���̻��� ������ ��ǥ�迡 ������, ȸ�������� �������� �����Ͽ� ��ȣ ���� �����ϴ� Queue�� �����Լ����ȴ�.
        // �������� ��ȣ ���� ���� ������ ���� ���� Queue�� �����Ͽ� AxmContiStart�Լ��� ����ؼ� �����Ѵ�.
        // dCenterPos = �߽��� X,Y  , dAngle = ����.
        // uCWDir   DIR_CCW(0): �ݽð����, DIR_CW(1) �ð����
        [DllImport(LibraryFileName)]
        private static extern uint AxmCircleAngleMove(int lCoord, ref int lAxisNo, ref double dCenterPos, double dAngle, double dVel, double dAccel, double dDecel, uint uCWDir);

        #endregion

        #region ���� ���� �Լ�

        //������ ��ǥ�迡 ���Ӻ��� �� ������ �����Ѵ�.
        //(����� ��ȣ�� 0 ���� ����))
        // ������: ������Ҷ��� �ݵ�� ���� ���ȣ�� ���� ���ں��� ū���ڸ� �ִ´�.
        //         ������ ���� �Լ��� ����Ͽ��� �� �������ȣ�� ���� ���ȣ�� ���� �� ���� lpAxesNo�� ���� ���ؽ��� �Է��Ͽ��� �Ѵ�.
        //         ������ ���� �Լ��� ����Ͽ��� �� �������ȣ�� �ش��ϴ� ���� ���ȣ�� �ٸ� ���̶�� �Ѵ�.
        //         SMC-2V03�� ��� lSize�� 2�� �Է��Ͽ��� �Ѵ�.
        //         ���� ���� �ٸ� Coordinate�� �ߺ� �������� ���ƾ� �Ѵ�.

        [DllImport(LibraryFileName)]
        private static extern uint AxmContiSetAxisMap(int lCoord, uint lSize, ref int lpRealAxesNo);
        //������ ��ǥ�迡 ���Ӻ��� �� ������ ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmContiGetAxisMap(int lCoord, ref uint lSize, ref int lpRealAxesNo);

        // ������ ��ǥ�迡 ���Ӻ��� �� ����/��� ��带 �����Ѵ�.
        // (������ : �ݵ�� ����� �ϰ� ��밡��)
        // ���� ���� �̵� �Ÿ� ��� ��带 �����Ѵ�.
        //uAbsRelMode : POS_ABS_MODE '0' - ���� ��ǥ��
        //              POS_REL_MODE '1' - ��� ��ǥ��

        [DllImport(LibraryFileName)]
        private static extern uint AxmContiSetAbsRelMode(int lCoord, uint uAbsRelMode);
        // ������ ��ǥ�迡 ���Ӻ��� �� ����/��� ��带 ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmContiGetAbsRelMode(int lCoord, ref uint upAbsRelMode);

        // ������ ��ǥ�迡 ���� ������ ���� ���� Queue�� ��� �ִ��� Ȯ���ϴ� �Լ��̴�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmContiReadFree(int lCoord, ref uint upQueueFree);
        // ������ ��ǥ�迡 ���� ������ ���� ���� Queue�� ����Ǿ� �ִ� ���� ���� ������ Ȯ���ϴ� �Լ��̴�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmContiReadIndex(int lCoord, ref int npQueueIndex);
        // ������ ��ǥ�迡 ���� ���� ������ ���� ����� ���� Queue�� ��� �����ϴ� �Լ��̴�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmContiWriteClear(int lCoord);

        // ������ ��ǥ�迡 ���Ӻ������� ������ �۾����� ����� �����Ѵ�. ���Լ��� ȣ������,
        // AxmContiEndNode�Լ��� ȣ��Ǳ� ������ ����Ǵ� ��� ����۾��� ���� ����� �����ϴ� ���� �ƴ϶� ���Ӻ��� ������� ��� �Ǵ� ���̸�,
        // AxmContiStart �Լ��� ȣ��� �� ��μ� ��ϵȸ���� ������ ����ȴ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmContiBeginNode(int lCoord);
        // ������ ��ǥ�迡�� ���Ӻ����� ������ �۾����� ����� �����Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmContiEndNode(int lCoord);

        // ���� ���� ���� �Ѵ�.
        // SMC-2V03 module :  dwProfileset, lAngle ���� 0���� �Է���. 
        // PCI-Nx04 : dwProfileset(CONTI_NODE_VELOCITY(0) : ���� ���� ���, CONTI_NODE_MANUAL(1) : �������� ���� ���, CONTI_NODE_AUTO(2) : �ڵ� �������� ����, 3 : �ӵ����� ��� ���) 
        [DllImport(LibraryFileName)]
        private static extern uint AxmContiStart(int lCoord, uint dwProfileset, int lAngle);
        // ������ ��ǥ�迡 ���� ���� ���� ������ Ȯ���ϴ� �Լ��̴�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmContiIsMotion(int lCoord, ref uint upInMotion);
        // ������ ��ǥ�迡 ���� ���� ���� �� ���� �������� ���� ���� �ε��� ��ȣ�� Ȯ���ϴ� �Լ��̴�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmContiGetNodeNum(int lCoord, ref int npNodeNum);
        // ������ ��ǥ�迡 ������ ���� ���� ���� �� �ε��� ������ Ȯ���ϴ� �Լ��̴�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmContiGetTotalNodeNum(int lCoord, ref int npNodeNum);

        #endregion

        #region Ʈ���� �Լ�

        // ���ǻ���: Ʈ���� ��ġ�� �����Ұ�� �ݵ�� UNIT/PULSE�� ���߾ �����Ѵ�.
        //           ��ġ�� UNIT/PULSE ���� �۰��� ��� �ּҴ����� UNIT/PULSE�� ���߾����⶧���� ����ġ�� ����Ҽ�����.

        // ���� �࿡ Ʈ���� ����� ��� ����, ��� ����, ��ġ �񱳱�, Ʈ���� ��ȣ ���� �ð� �� Ʈ���� ��� ��带 �����Ѵ�.
        // Ʈ���� ��� ����� ���ؼ��� ����  AxmTriggerSetTimeLevel �� ����Ͽ� ���� ��� ������ ���� �Ͽ��� �Ѵ�.
        // dTrigTime        : Ʈ���� ��� �ð� 
        //                    1usec - �ִ� 50msec ( 1 - 50000 ���� ����)
        // upTriggerLevel   : Ʈ���� ��� ���� ����   => LOW(0), HIGH(1)
        // uSelect          : ����� ���� ��ġ        => COMMAND(0), ACTUAL(1)
        // uInterrupt       : ���ͷ�Ʈ ����           => DISABLE(0), ENABLE(1)

        // ���� �࿡ Ʈ���� ��ȣ ���� �ð� �� Ʈ���� ��� ����, Ʈ���� ��¹���� �����Ѵ�.
        [DllImport(LibraryFileName)]
        public static extern uint AxmTriggerSetTimeLevel(int lAxisNo, double dTrigTime, uint uTriggerLevel, uint uSelect, uint uInterrupt);
        // ���� �࿡ Ʈ���� ��ȣ ���� �ð� �� Ʈ���� ��� ����, Ʈ���� ��¹���� ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmTriggerGetTimeLevel(int lAxisNo, ref double dTrigTime, ref uint uTriggerLevel, ref uint uSelect, ref uint uInterrupt);

        // ���� ���� Ʈ���� ��� ����� �����Ѵ�.
        // uMethod : PERIOD_MODE      0x0 : ���� ��ġ�� �������� dPos�� ��ġ �ֱ�� ����� �ֱ� Ʈ���� ���
        //           ABS_POS_MODE     0x1 : Ʈ���� ���� ��ġ���� Ʈ���� �߻�, ���� ��ġ ���

        // dPos    : �ֱ� ���ý� : ��ġ������ġ���� ����ϱ⶧���� �� ��ġ
        //           ���� ���ý� : ����� �� ��ġ, �� ��ġ�Ͱ����� ������ ����� ������. 
        // ���ǻ���: N404, N804�� ��쿡�� AxmTriggerSetAbsPeriod�� �ֱ���� �����Ұ�� ó�� ����ġ�� ���� �ȿ� �����Ƿ� 
        //                              Ʈ���� ����� �ѹ� �߻��Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmTriggerSetAbsPeriod(int nAxisNo, uint uMethod, double dPos);

        // ���� �࿡ Ʈ���� ����� ��� ����, ��� ����, ��ġ �񱳱�, Ʈ���� ��ȣ ���� �ð� �� Ʈ���� ��� ��带 ��ȯ�Ѵ�.
        // ���ǻ���: IP������ AxmTriiggerSetBlock�Լ��� ȣ��� ���ζ��̺귯������ �������� ABS_POS_MODE�� ����ϱ� ������ 
        // ���Լ��� ��ȯ�ϴ°��� 1�� ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmTriggerGetAbsPeriod(int nAxisNo, ref uint upMethod, ref double dpPos);

        //  ����ڰ� ������ ������ġ���� ������ġ���� ������������ Ʈ���Ÿ� ��� �Ѵ�.
        [DllImport(LibraryFileName)]
        public static extern uint AxmTriggerSetBlock(int nAxisNo, double dStartPos, double dEndPos, double dPeriodPos);
        // 'AxmTriggerSetBlock' �Լ��� Ʈ���� ������ ���� �д´�..
        [DllImport(LibraryFileName)]
        private static extern uint AxmTriggerGetBlock(int nAxisNo, ref double dpStartPos, ref double dpEndPos, ref double dpPeriodPos);
        // ����ڰ� �� ���� Ʈ���� �޽��� ����Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmTriggerOneShot(int nAxisNo);
        // ����ڰ� �� ���� Ʈ���� �޽��� �����Ŀ� ����Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmTriggerSetTimerOneshot(int nAxisNo, int mSec);
        // ������ġ Ʈ���� ���Ѵ� ������ġ ����Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmTriggerOnlyAbs(int nAxisNo, int nTrigNum, double[] dTrigPos);
        // Ʈ���� ������ �����Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmTriggerSetReset(int nAxisNo);

        #endregion

        #region CRC( �ܿ� �޽� Ŭ���� �Լ�)

        //Level   : LOW(0), HIGH(1), UNUSED(2), USED(3)
        //uMethod : �ܿ��޽� ���� ��� ��ȣ �޽� �� 2 - 6���� ��������.
        //          0: Don't care , 1: Don't care, 2: 500 uSec, 3: 1 mSec, 4: 10 mSec, 5: 50 mSec, 6: 100 mSec

        //���� �࿡ CRC ��ȣ ��� ���� �� ��� ������ �����Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmCrcSetMaskLevel(int nAxisNo, uint uLevel, uint uMethod);
        // ���� ���� CRC ��ȣ ��� ���� �� ��� ������ ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmCrcGetMaskLevel(int nAxisNo, ref uint upLevel, ref uint upMethod);

        //uOnOff  : CRC ��ȣ�� Program���� �߻� ����  (FALSE(0),TRUE(1))

        // ���� �࿡ CRC ��ȣ�� ������ �߻� ��Ų��.
        [DllImport(LibraryFileName)]
        private static extern uint AxmCrcSetOutput(int nAxisNo, uint uOnOff);
        // ���� ���� CRC ��ȣ�� ������ �߻� ���θ� ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmCrcGetOutput(int nAxisNo, ref uint upOnOff);

        //-----------	SMC-2V03 module ���� �Լ� : EndLimit�� ������ ������ ��ȣ�� �߻���Ų��. --------
        // uPositiveUse : Positive Emeregency End limit�� ���� Clear��� ��� ����
        // uNegativeUse : Negative Emeregency End limit�� ���� Clear��� ��� ����
        // Level   : LOW(0), HIGH(1), UNUSED(2)
        // ���� �࿡ ����Ʈ�� ���� CRC ��ȣ�� ��� ���� �� ��� ������ �����Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmCrcSetEndLimit(int nAxisNo, uint uPositiveLevel, uint uNegativeLevel);
        // ���� ���� ����Ʈ�� ���� CRC ��ȣ�� ��� ���� �� ��� ������ ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmCrcGetEndLimit(int nAxisNo, ref uint upPositiveLevel, ref uint upNegativeLevel);

        #endregion

        #region MPG(Manual Pulse Generation) �Լ�

        //================ SMC-2V03 module ===========================================================
        // lInputMethod : 0-7 ���� ��������. 0:OnePhase, 1:TwoPhase1, 2:TwoPhase2, 3:TwoPhase4
        //                                   4:Level One Phase, 5:Level Two Phase1, 6: Level Two Phase2, 7:Level Two Phase4
        // lDriveMode   : 0-2 ���� �������� (0 :MPG �����̺� ��� ,1 :MPG PRESET ���, 2 :MPG ���� ���)
        // MPGPos		: MPG �Է½�ȣ���� �̵��ϴ� �Ÿ�
        // dMPGdenominator, dMPGnumerator ������.


        //================ PCI-Nx04 ============================================================
        // lInputMethod : 0-3 ���� ��������. 0:OnePhase, 1:TwoPhase1(IP������, QI��������) , 2:TwoPhase2, 3:TwoPhase4
        // lDriveMode   : 0�� �������� (0 :MPG ���Ӹ��)
        // MPGPos		: MPG �Է½�ȣ���� �̵��ϴ� �Ÿ�
        // MPGdenominator: MPG(���� �޽� �߻� ��ġ �Է�)���� �� ������ ��
        // dMPGnumerator : MPG(���� �޽� �߻� ��ġ �Է�)���� �� ���ϱ� ��
        // dwNumerator   : �ִ�(1 ����    64) ���� ���� ����
        // dwDenominator : �ִ�(1 ����  4096) ���� ���� ����
        // dMPGdenominator = 4096, MPGnumerator=1 �� �ǹ��ϴ� ���� 
        // MPG �ѹ����� 200�޽��� �״�� 1:1�� 1�޽��� ����� �ǹ��Ѵ�. 
        // ���� dMPGdenominator = 4096, MPGnumerator=2 �� �������� 1:2�� 2�޽��� ����� �������ٴ��ǹ��̴�. 
        // ���⿡ MPG PULSE = ((Numerator) * (Denominator)/ 4096 ) Ĩ���ο� ��³����� �����̴�.

        // ���� �࿡ MPG �Է¹��, ����̺� ���� ���, �̵� �Ÿ�, MPG �ӵ� ���� �����Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMPGSetEnable(int nAxisNo, int nInputMethod, int nDriveMode, double dMPGPos, double dVel, double dAccel);
        // ���� �࿡ MPG �Է¹��, ����̺� ���� ���, �̵� �Ÿ�, MPG �ӵ� ���� ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMPGGetEnable(int nAxisNo, ref int npInputMethod, ref int npDriveMode, ref double dpMPGPos, ref double dpVel);

        // IP ������, QI ���� �Լ�.
        // ���� �࿡ MPG ����̺� ���� ��忡�� ���޽��� �̵��� �޽� ������ �����Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMPGSetRatio(int nAxisNo, double dMPGnumerator, double dMPGdenominator);
        // ���� �࿡ MPG ����̺� ���� ��忡�� ���޽��� �̵��� �޽� ������ ��ȯ�Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMPGGetRatio(int nAxisNo, ref double dMPGnumerator, ref double dMPGdenominator);

        // ���� �࿡ MPG ����̺� ������ �����Ѵ�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmMPGReset(int nAxisNo);

        #endregion

        #region �︮�� �̵�  (PCI-Nx04 ���� �Լ�)
        // ������ ��ǥ�迡 ������, �������� �߽����� �����Ͽ� �︮�� ���� �����ϴ� �Լ��̴�.
        // AxmContiBeginNode, AxmContiEndNode�� ���̻��� ������ ��ǥ�迡 ������, �������� �߽����� �����Ͽ� �︮�� ���Ӻ��� �����ϴ� �Լ��̴�. 
        // ��ȣ ���� ���� ������ ���� ���� Queue�� �����ϴ� �Լ��̴�. AxmContiStart�Լ��� ����ؼ� �����Ѵ�. (���Ӻ��� �Լ��� ���� �̿��Ѵ�)
        // dCenterPos = �߽��� X,Y  , dEndPos = ������ X,Y 	
        // uCWDir   DIR_CCW(0): �ݽð����, DIR_CW(1) �ð����	
        [DllImport(LibraryFileName)]
        private static extern uint AxmHelixCenterMove(int lCoord, double dCenterXPos, double dCenterYPos, double dEndXPos, double dEndYPos, double dZPos, double dVel, double dAccel, double dDecel, uint uCWDir);
        // ������ ��ǥ�迡 ������, �������� �������� �����Ͽ� �︮�� ���� �����ϴ� �Լ��̴�. 
        // AxmContiBeginNode, AxmContiEndNode�� ���̻��� ������ ��ǥ�迡 �߰���, �������� �����Ͽ� �︮�ÿ��� ���� �����ϴ� �Լ��̴�. 
        // ��ȣ ���� ���� ������ ���� ���� Queue�� �����ϴ� �Լ��̴�. AxmContiStart�Լ��� ����ؼ� �����Ѵ�. (���Ӻ��� �Լ��� ���� �̿��Ѵ�.)
        // dMidPos = �߰��� X,Y  , dEndPos = ������ X,Y 
        [DllImport(LibraryFileName)]
        private static extern uint AxmHelixPointMove(int lCoord, double dMidXPos, double dMidYPos, double dEndXPos, double dEndYPos, double dZPos, double dVel, double dAccel, double dDecel);
        // ������ ��ǥ�迡 ������, �������� �������� �����Ͽ� �︮�� ���� �����ϴ� �Լ��̴�.
        // AxmContiBeginNode, AxmContiEndNode�� ���̻��� ������ ��ǥ�迡 ������, �������� �������� �����Ͽ� �︮�ÿ��� ���� �����ϴ� �Լ��̴�. 
        // ��ȣ ���� ���� ������ ���� ���� Queue�� �����ϴ� �Լ��̴�. AxmContiStart�Լ��� ����ؼ� �����Ѵ�. (���Ӻ��� �Լ��� ���� �̿��Ѵ�.)
        // dRadius = ������, dEndPos = ������ X,Y  , uShortDistance = ������(0), ū��(1)
        // uCWDir   DIR_CCW(0): �ݽð����, DIR_CW(1) �ð����	
        [DllImport(LibraryFileName)]
        private static extern uint AxmHelixRadiusMove(int lCoord, double dRadius, double dEndXPos, double dEndYPos, double dZPos, double dVel, double dAccel, double dDecel, uint uCWDir, uint uShortDistance);
        // ������ ��ǥ�迡 ������, ȸ�������� �������� �����Ͽ� �︮�� ���� �����ϴ� �Լ��̴�
        // AxmContiBeginNode, AxmContiEndNode�� ���̻��� ������ ��ǥ�迡 ������, ȸ�������� �������� �����Ͽ� �︮�ÿ��� ���� �����ϴ� �Լ��̴�. 
        // ��ȣ ���� ���� ������ ���� ���� Queue�� �����ϴ� �Լ��̴�. AxmContiStart�Լ��� ����ؼ� �����Ѵ�. (���Ӻ��� �Լ��� ���� �̿��Ѵ�.)
        //dCenterPos = �߽��� X,Y  , dAngle = ����.
        // uCWDir   DIR_CCW(0): �ݽð����, DIR_CW(1) �ð����	
        [DllImport(LibraryFileName)]
        private static extern uint AxmHelixAngleMove(int lCoord, double dCenterXPos, double dCenterYPos, double dAngle, double dZPos, double dVel, double dAccel, double dDecel, uint uCWDir);
        #endregion

        #region ���ö��� �̵� (PCI-Nx04 ���� �Լ�)
        // AxmContiBeginNode, AxmContiEndNode�� ���̻�����. 
        // ���ö��� ���� ���� �����ϴ� �Լ��̴�. ��ȣ ���� ���� ������ ���� ���� Queue�� �����ϴ� �Լ��̴�.
        // AxmContiStart�Լ��� ����ؼ� �����Ѵ�. (���Ӻ��� �Լ��� ���� �̿��Ѵ�.)	
        // lPosSize : �ּ� 3�� �̻�.
        // 2������ ���� dPoZ���� 0���� �־��ָ� ��.
        // 3������ ���� ������� 3���� dPosZ ���� �־��ش�.
        [DllImport(LibraryFileName)]
        private static extern uint AxmSplineWrite(int lCoord, int lPosSize, ref double dPosX, ref double dPosY, double dVel, double dAccel, double dDecel, double dPosZ, int lPointFactor);
        #endregion

        #region �������� ����

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
        #region ��� �Ķ��Ÿ ����
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

        // 보드에서 Setup/Config 동기화에 필요한 Get 헬퍼들. ReadSetupFromBoard 에서 사용.
        public static int GetAccelerationJerk(int axis, ref double accelerationJerk)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmMotGetAccelJerk", AXM.AxmMotGetAccelJerk(axis, ref accelerationJerk))) != 0) return ret;
            return ret;
        }

        public static int GetDecelerationJerk(int axis, ref double decelerationJerk)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmMotGetDecelJerk", AXM.AxmMotGetDecelJerk(axis, ref decelerationJerk))) != 0) return ret;
            return ret;
        }

        /// <summary>축의 ProfileMode raw 값(0~4 AXL 사양)을 반환.</summary>
        public static int GetProfileModeRaw(int axis, ref uint mode)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmMotGetProfileMode", AXM.AxmMotGetProfileMode(axis, ref mode))) != 0) return ret;
            return ret;
        }

        /// <summary>EMG 정지 신호의 정지모드/액티브레벨을 반환.</summary>
        public static int GetSignalStop(int axis, ref uint stopMode, ref ActiveLevel level)
        {
            int ret = 0;
            uint uLevel = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalGetStop", AXM.AxmSignalGetStop(axis, ref stopMode, ref uLevel))) != 0) return ret;
            level = uLevel == 0 ? ActiveLevel.Low : ActiveLevel.High;
            return ret;
        }

        /// <summary>리밋 정지 모드(0:EMG, 1:Slowdown)를 반환.</summary>
        public static int GetLimitStopMode(int axis, ref uint stopMode)
        {
            int ret = 0;
            uint pos = 0, neg = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalGetLimit", AXM.AxmSignalGetLimit(axis, ref stopMode, ref pos, ref neg))) != 0) return ret;
            return ret;
        }

        /// <summary>소프트리밋 Use(0:Disable, 1:Enable) 플래그를 반환.</summary>
        public static int GetSoftLimitEnable(int axis, ref uint use)
        {
            int ret = 0;
            uint stop = 0, mode = 0;
            double pos = 0, neg = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalGetSoftLimit", AXM.AxmSignalGetSoftLimit(axis, ref use, ref stop, ref mode, ref pos, ref neg))) != 0) return ret;
            return ret;
        }

        /// <summary>소프트리밋 Use 플래그(0:Disable,1:Enable)만 변경. 다른 값은 보드 현재값 유지.</summary>
        public static int SetSoftLimitEnable(int axis, bool enable)
        {
            int ret = 0;
            uint use = 0, stop = 0, mode = 0;
            double pos = 0, neg = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalGetSoftLimit", AXM.AxmSignalGetSoftLimit(axis, ref use, ref stop, ref mode, ref pos, ref neg))) != 0) return ret;
            use = enable ? 1u : 0u;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalSetSoftLimit", AXM.AxmSignalSetSoftLimit(axis, use, stop, mode, pos, neg))) != 0) return ret;
            return ret;
        }

        /// <summary>소프트리밋 전체 값(Use/Pos/Neg)을 한 번에 설정.</summary>
        public static int SetSoftLimits(int axis, bool enable, double positive, double negative)
        {
            int ret = 0;
            uint use = 0, stop = 0, mode = 0;
            double curPos = 0, curNeg = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalGetSoftLimit", AXM.AxmSignalGetSoftLimit(axis, ref use, ref stop, ref mode, ref curPos, ref curNeg))) != 0) return ret;
            use = enable ? 1u : 0u;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalSetSoftLimit", AXM.AxmSignalSetSoftLimit(axis, use, stop, mode, positive, negative))) != 0) return ret;
            return ret;
        }

        /// <summary>리밋 정지 모드(0:EMG, 1:Slowdown)만 변경. 레벨 값은 보드 현재값 유지.</summary>
        public static int SetLimitStopMode(int axis, uint stopMode)
        {
            int ret = 0;
            uint stop = 0, pos = 0, neg = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalGetLimit", AXM.AxmSignalGetLimit(axis, ref stop, ref pos, ref neg))) != 0) return ret;
            if ((ret = AXL.CheckErrorCode("AXM.AxmSignalSetLimit", AXM.AxmSignalSetLimit(axis, stopMode, pos, neg))) != 0) return ret;
            return ret;
        }

        #endregion

        #region ����� ��ȣ ���� �����Լ�
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

        #region ��� ������ �� �����Ŀ� ���� Ȯ���ϴ� �Լ�
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

        public static int GetAxisInfo(int axisNo, ref int nodeNum, ref int modulePosition, ref uint moduleId)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmInfoGetAxis", AXM.AxmInfoGetAxis(axisNo, ref nodeNum, ref modulePosition, ref moduleId))) != 0) return ret;
            return ret;
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

        #region Ȩ���� �Լ�
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

        #region ��ġ�����Լ�
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

        #region �������̵� �Լ�
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

        #region ��Ʈ�� ���� �Լ�
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

        #region ���� ���� �� ���� ���� �Լ�
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

        #region ���� ���� ���� �� ���� �Լ�
        public static int SetPathAxisMap(int coordinate, int[] axes)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmContiSetAxisMap", AXM.AxmContiSetAxisMap(coordinate, (uint)axes.Length, ref axes[0]))) != 0) return ret;
            return ret;
        }

        public static int GetPathAxisMap(int coordinate, ref uint size, int[] axes)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmContiGetAxisMap", AXM.AxmContiGetAxisMap(coordinate, ref size, ref axes[0]))) != 0) return ret;
            return ret;
        }

        public static int SetPathAbsRelMode(int coordinate, AXT_MOTION_ABSREL mode)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmContiSetAbsRelMode", AXM.AxmContiSetAbsRelMode(coordinate, (uint)mode))) != 0) return ret;
            return ret;
        }

        public static int GetPathAbsRelMode(int coordinate, ref AXT_MOTION_ABSREL mode)
        {
            int ret = 0;
            uint value = 0;
            if ((ret = AXL.CheckErrorCode("AXM.AxmContiGetAbsRelMode", AXM.AxmContiGetAbsRelMode(coordinate, ref value))) != 0) return ret;
            mode = (AXT_MOTION_ABSREL)value;
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

        #region ���ͷ�Ʈ �Լ�
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

        #region Ʈ���� �Լ�
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
