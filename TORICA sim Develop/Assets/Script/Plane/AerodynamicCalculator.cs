using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Text;
using System.IO;
using System;

public class AerodynamicCalculator : SerialReceive
{
    //設計データ書き込み用変数
    private string path;//ファイルパス
    private string fileName = "data.csv";//ファイル名
    public static List<List<string>> CsvList = new List<List<string>>();//CSVファイルリスト
    private bool CanReadCsv = false;

    // public
    [System.NonSerialized] public float Airspeed = 0.000f; // Airspeed [m/s]
    [System.NonSerialized] public float alpha = 0.000f; // Angle of attack [deg]
    [System.NonSerialized] public float beta = 0.000f; // Side slip angle [deg]
    [System.NonSerialized] public float de = 0.000f; // Elevator angle [deg]
    [System.NonSerialized] public float dr = 0.000f; // Rudder angle [deg]
    [System.NonSerialized] public float dh = 0.000f; // Movement of c.g. [-]
    [System.NonSerialized] public float LocalGustMag = 0.000f; // Magnitude of local gust [m/s]
    [System.NonSerialized] public float LocalGustDirection = 0.000f; // Magnitude of local gust [m/s]
    [System.NonSerialized] public float nz = 0.000f; // Load factor [-]

    [System.NonSerialized] public float Groundspeed = 0.000f; // Groundspeed [m/s]
    [System.NonSerialized] public float ALT = 0.000f;

    //各センサーにかかる荷重
    [System.NonSerialized] public float massLeftRaw;
    [System.NonSerialized] public float massRightRaw;
    [System.NonSerialized] public float massBackwardLeftRaw;
    [System.NonSerialized] public float massBackwardRightRaw;

    [System.NonSerialized] public float pitchGravity = 0.000f;//ピッチ重心計算結果[m]
    [System.NonSerialized] public float pitchGravityPilot = 0.2f;//ピッチ重心計算結果[m]
    [System.NonSerialized] public float pitchGravityPilotS;//定常状態(pitchGravity=0)のパイロット重心

    [System.NonSerialized] public float massLeftRightS;//定常状態の前センサーの値(合計値ではなく一つのセンサーの値)
    [System.NonSerialized] public float massBackwardS;//定常状態の後センサーの値(合計値ではなく一つのセンサーの値)
    // Phisics
    static private float rho = 1.164f;
    static private float hE0 = 10.500f; // Altitude at Take-off [m]
    // At Cruise without Ground Effect
    static private float Airspeed0; // Magnitude of ground speed [m/s]
    static private float alpha0; // Angle of attack [deg]
    static private float CDp0; // Parasitic drag [-]
    static private float Cmw0; // Pitching momentum [-]
    static private float CLMAX; // Lift Coefficient [-]
    static private float CL0 = 0.000f; // Lift Coefficient [-]
    static private float CLw0 = 0.000f; // Lift Coefficient [-]
    static private float CLt0 = 0.000f; // Tail Coefficient [-]
    static private float epsilon0 = 0.000f; // Downwash
    // Plane
    static bool Downwash; // Conventional Tail: True, T-Tail: False
    static private float CL = 0.000f; // Lift Coefficient [-]
    static private float CD = 0.000f; // Drag Coefficient [-]
    static private float Cx = 0.000f; // X Force Coefficient [-]
    static private float Cy = 0.000f; // Y Force Coefficient [-]
    static private float Cz = 0.000f; // Z Force Coefficient [-]
    static private float Cl = 0.000f; // Rolling momentum [-]
    static private float Cm = 0.000f; // Pitching momentum [-]
    static private float Cn = 0.000f; // Yawing momentum [-]
    static private float dh0 = 0.000f; // Initial Mouse Position
    // Wing
    static private float Sw; // Wing area of wing [m^2]
    static private float bw; // Wing span [m]
    static private float cMAC; // Mean aerodynamic chord [m]
    static public float aw; // Wing Lift Slope [1/deg]
    
    static private float ac;
    static private float cg;

    static private float hw; // Length between Wing a.c. and c.g. [-] ac-cg

    static private float hw0;
    static private float lt0;

    static private float AR; // Aspect Ratio [-]
    static private float ew; // Wing efficiency [-]
    static private float CLw = 0.000f; // Lift Coefficient [-]
    // Tail
    static private float St; // Wing area of tail [m^2]
    static private float at; // Tail Lift Slope [1/deg]
    static private float lt; // Length between Tail a.c. and c.g. [m]
    static private float VH; // Tail Volume [-]
    static private float deMAX; // Maximum elevator angle [deg]
    static private float tau; // Control surface angle of attack effectiveness [-]
    static private float CLt = 0.000f; // Lift Coefficient [-]
    // Fin
    static private float drMAX; // Maximum rudder angle
    // Ground Effect
    static private float CGEMIN; // Minimum Ground Effect Coefficient [-]
    static private float CGE = 0f; // Ground Effect Coefficient: CDiGE/CDi [-]
    // Stability derivatives
    static private float Cyb; // [1/deg]
    static private float Cyp; // [1/rad]
    static private float Cyr; // [1/rad]
    static private float Cydr; // [1/deg]
    static private float Cnb; // [1/deg]
    static private float Cnp; // [1/rad]
    static private float Cnr; // [1/rad]
    static private float Cndr; // [1/deg]
    static private float Clb; // [1/deg]
    static private float Clp; // [1/rad]
    static private float Clr; // [1/rad]
    static private float Cldr; // [1/deg]
    // Gust
    static private Vector3 Gust = Vector3.zero; // Gust [m/s]
    // Rotation
    static private float phi; // [deg]
    static private float theta;  // [deg]
    static private float psi; // [deg]

    private Rigidbody PlaneRigidbody;

    //計算で用いるセンサー値
    static private float massLeft;//左ひずみの値[kg]
    static private float massRight;//右ひずみの値[kg]
    static private float massBackward;//後方ひずみの値[kg]

    //追加機体データ
    static private float lengthForward;//フレーム前方(フレーム＋センサー部分)から桁(原点)位置[m]
    static private float lengthBackward;//フレーム後方(フレームの端)から桁(原点)位置[m]
    static private float aircraftCenterOfMass;//機体のみ全重心(パイロットなし,ピッチのみ)[m]
    static private float aircraftMass;//機体のみ全重量[kg]
    static private float pilotMass;//パイロット重量[kg]
    static private float SensorPositionY = 1.0f;//桁中心から垂直に線を超音波センサーの位置までおろした時の線の長さ[m]
    static private float SensorPositionZ = 0.0f;//↑の到達点から超音波センサーまでの長さ[m]
    //計算結果データ
    static private float hw2;//	主翼空力中心と全機重心の距離（cMACで無次元化）（再計算バージョン）

    //翼持ちデータ
    static private float YMin;//翼持ちの最小荷重(機体のみ重量/2)
    static private float YrMax;//右翼持ちの許容最大荷重
    static private float YlMax;//左翼持ちの許容最大荷重
    static private float YrMoment;//右翼持ち本人がかけるモーメント
    static private float YlMoment;//左翼持ち本人がかけるモーメント

    static private float YL;//機体中心から翼持ち棒までの長さ[m]

    public static GameObject Aircraft;

    private bool AddTaleForce;

    public void OnEnables()
    {
        if(MyGameManeger.instance.PlaneName != null){
            if(this.gameObject.name == MyGameManeger.instance.PlaneName)
            {
                Aircraft = this.gameObject;
            }
        }
        else{
            if(this.gameObject.name == MyGameManeger.instance.DefaultPlane)
            {
                MyGameManeger.instance.PlaneName = MyGameManeger.instance.DefaultPlane;
                Aircraft = this.gameObject;
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // Get rigidbody component
        PlaneRigidbody = this.GetComponent<Rigidbody>();
        this.transform.rotation = Quaternion.Euler(0.0f, MyGameManeger.instance.StartRotation, 0.0f);
        
        //設計データ読み込み用
        fileName = MyGameManeger.instance.PlaneName + ".csv";
        path = Application.dataPath + "/" + fileName;
        Debug.Log("File path: " + path);
        ReadFile();
        
        // Input Specifications
        InputSpecifications();

        //pitchGravityPilotS = ((PlaneRigidbody.mass*pitchGravity)-(aircraftMass*aircraftCenterOfMass))/pilotMass;
        pitchGravityPilotS = -aircraftMass*aircraftCenterOfMass/pilotMass;
        massLeftRightS = (pilotMass*(pitchGravityPilotS+lengthBackward)/(lengthForward+lengthBackward))/2;
        massBackwardS = (pilotMass - massLeftRightS*2)/2;

        YMin = aircraftMass/2;
        YrMax = 80.0f;
        YlMax = 80.0f;
        
        // Set take-off speed
        if(MyGameManeger.instance.FlightMode=="BirdmanRally"){
            //MyGameManeger.instance.Airspeed_TO = 5.0f; // Airspeed at take-off [m/s]
            PlaneRigidbody.velocity = Vector3.zero;
        }else if(MyGameManeger.instance.FlightMode=="TestFlight"){ //
            PlaneRigidbody.velocity = new Vector3(
                Airspeed0*Mathf.Cos(Mathf.Deg2Rad*alpha0)*Mathf.Cos(Mathf.Deg2Rad*MyGameManeger.instance.StartRotation),
                -Airspeed0*Mathf.Sin(Mathf.Deg2Rad*alpha0),
                -Airspeed0*Mathf.Cos(Mathf.Deg2Rad*alpha0)*Mathf.Sin(Mathf.Deg2Rad*MyGameManeger.instance.StartRotation)
            );
        }

        // Calculate CL at cluise
        CL0 = (PlaneRigidbody.mass*Physics.gravity.magnitude)/(0.5f*rho*Airspeed0*Airspeed0*Sw);
        CLt0 = (Cmw0+CL0*hw)/(VH+(St/Sw)*hw);
        CLw0 = CL0-(St/Sw)*CLt0;
        if(Downwash){epsilon0 = (CL0/(Mathf.PI*ew*AR))*Mathf.Rad2Deg;}

        dh0 = Screen.height/2f; // Initial Mouse Position

        //Debug.Log(CLw0);
        hw0 =hw;
        lt0 =lt;

        //massBackwardNow=28000f;
        //massLeftNow=10000f;
        //massRightNow=10000f;
    }

    //重心フレーム開発の名残
    void Update()
    {
        if (MyGameManeger.instance.MousePitchControl){
            pitchGravityPilot = pitchGravityPilotS + (((Input.mousePosition.y-dh0)*MyGameManeger.instance.MouseSensitivity)/250.0f)*0.10f;
            pitchGravity = ((pitchGravityPilot*pilotMass)+(aircraftCenterOfMass*aircraftMass))/(pilotMass+aircraftMass);
        }

        if(Input.GetAxisRaw("GStick") != 0){
            pitchGravityPilot = pitchGravityPilotS -Input.GetAxisRaw("GStick")*0.10f;
            pitchGravity = ((pitchGravityPilot*pilotMass)+(aircraftCenterOfMass*aircraftMass))/(pilotMass+aircraftMass);
        }
        //pitchGravity = ((pitchGravityPilot*pilotMass)+(aircraftCenterOfMass*aircraftMass))/(pilotMass+aircraftMass);

        if(MyGameManeger.instance.FrameUseable)
        {
            /*
            massLeftNow = 20000f;
            massRightNow = 20000f;
            massBackwardRightNow = 20000f;
            massBackwardLeftNow = 20000f;
            */

            //mass~Now ←センサー生データ
            //mass~0 ←オフセット,現在は使用されていません
            //mass~Raw ←生データにFactorをかけて補正したもの
            //mass~Factor ←Rawを調整するための係数
            //mass~ ←計算用にパイロットの体重に換算したもの
            
            massRightRaw = MyGameManeger.instance.massRightFactor*(massRightNow/1000);
            massLeftRaw = MyGameManeger.instance.massLeftFactor*(massLeftNow/1000);
            massBackwardRightRaw = MyGameManeger.instance.massBackwardRightFactor*(massBackwardRightNow/1000);
            massBackwardLeftRaw = MyGameManeger.instance.massBackwardLeftFactor*(massBackwardLeftNow/1000);

            float NowMass = massLeftRaw + massRightRaw + massBackwardLeftRaw + massBackwardRightRaw;

            //float e = 1;//本来のP以外が搭乗したときの補正用係数
            //e = pilotMass/NowMass;
            //Debug.Log("e="+e);
            
            massRight=massRightRaw;
            massLeft=massLeftRaw;
            massBackward=(massBackwardLeftRaw+massBackwardRightRaw);

            //リジットボディに代入するピッチの値を計算
            pitchGravity = (((lengthForward*massLeft)+(lengthForward*massRight)-(lengthBackward*massBackward)+(aircraftCenterOfMass*aircraftMass))/(massLeft+massRight+massBackward+aircraftMass));
            pitchGravityPilotS = ((PlaneRigidbody.mass*pitchGravity)-(aircraftMass*aircraftCenterOfMass))/pilotMass;
            if(NowMass != 0 ){
                pitchGravityPilot = (((lengthForward*massLeft)+(lengthForward*massRight)-(lengthBackward*massBackward))/(massLeft+massRight+massBackward)); 
            }
            else{
                pitchGravityPilot = pitchGravityPilotS;
            }
        }
        //リジットボディに代入
        PlaneRigidbody.centerOfMass = new Vector3(pitchGravity,PlaneRigidbody.centerOfMass.y,PlaneRigidbody.centerOfMass.z);

        //hwに代入する重心位置(%MAC)を計算
        hw2= hw0-(pitchGravity*0.85f/cMAC);
        //hwに代入
        hw = hw2;

        lt = lt0 + pitchGravity*0.85f;

        // Get control surface angles
        de = 0.000f;
        dr = 0.000f;

        if(!MyGameManeger.instance.FrameUseable){
            de = Input.GetAxisRaw("Vertical")*deMAX;
            dr = -Input.GetAxisRaw("Horizontal")*drMAX;
        }
        if(Input.GetMouseButton(0)){dr = drMAX;}
        else if(Input.GetMouseButton(1)){dr = -drMAX;}
        
        if(Input.GetAxisRaw("Trigger")*drMAX != 0){
            dr = -Input.GetAxisRaw("Trigger")*drMAX;
        }

        if(MyGameManeger.instance.FrameUseable){
            //↓必要な処理
            dr = ((JoyStickNow-MyGameManeger.instance.JoyStick0)/500)*drMAX;
        }
    }
    
    void FixedUpdate()
    {
        // Velocity and AngularVelocity
        float u = transform.InverseTransformDirection(PlaneRigidbody.velocity).x;
        float v = -transform.InverseTransformDirection(PlaneRigidbody.velocity).z;
        float w = -transform.InverseTransformDirection(PlaneRigidbody.velocity).y;
        float p = -transform.InverseTransformDirection(PlaneRigidbody.angularVelocity).x*Mathf.Rad2Deg;
        float q = transform.InverseTransformDirection(PlaneRigidbody.angularVelocity).z*Mathf.Rad2Deg;
        float r = transform.InverseTransformDirection(PlaneRigidbody.angularVelocity).y*Mathf.Rad2Deg;
        float hE = PlaneRigidbody.position.y;

        // Force and Momentum
        Vector3 AerodynamicForce = Vector3.zero;
        Vector3 AerodynamicMomentum = Vector3.zero;
        Vector3 TakeoffForce = Vector3.zero;

        // Hoerner and Borst (Modified)
        CGE = (CGEMIN+33f*Mathf.Pow((hE/bw),1.5f))/(1f+33f*Mathf.Pow((hE/bw),1.5f));
        
        //if (MyGameManeger.instance.MousePitchControl){
        //    dh = -(Input.mousePosition.y-dh0)*0.0002f*MyGameManeger.instance.MouseSensitivity;
        //}

        // Gust
        LocalGustMag = MyGameManeger.instance.GustMag*Mathf.Pow((hE/hE0),1f/7f);
        Gust = Quaternion.AngleAxis(MyGameManeger.instance.GustDirection,Vector3.up)*(Vector3.right*LocalGustMag);
        Vector3 LocalGust = this.transform.InverseTransformDirection(Gust);
        float ug = LocalGust.x + 1e-10f;
        float vg = -LocalGust.z;
        float wg = -LocalGust.y;
        if(ug>0){LocalGustDirection = Mathf.Atan(vg/(ug+1e-10f))*Mathf.Rad2Deg;}
        else{LocalGustDirection = Mathf.Atan(vg/(ug+1e-10f))*Mathf.Rad2Deg+vg/Mathf.Abs((vg+1e-10f))*180;}

        // Calculate angles
        Airspeed =    Mathf.Sqrt((u+ug)*(u+ug) + (v+vg)*(v+vg)+(w+wg)*(w+wg));
        Groundspeed = Mathf.Sqrt(u*u + v*v);
        ALT = PlaneRigidbody.position.y - SensorPositionY;
        //Debug.Log(Groundspeed);
        alpha = Mathf.Atan((w+wg)/(u+ug))*Mathf.Rad2Deg;
        //Debug.Log(alpha);
        
        beta = Mathf.Atan((v+vg)/Airspeed)*Mathf.Rad2Deg;

        // Wing and Tail
        CLw = CLw0+aw*(alpha-alpha0);
        CLt = CLt0+at*((alpha-alpha0)+(1f-CGE*(CLw/CLw0))*epsilon0+de*tau+((lt-dh*cMAC)/Airspeed)*q);
        if(Mathf.Abs(CLw)>CLMAX){CLw = (CLw/Mathf.Abs(CLw))*CLMAX;} // Stall
        if(Mathf.Abs(CLt)>CLMAX){CLt = (CLt/Mathf.Abs(CLt))*CLMAX;} // Stall

        // Lift and Drag
        CL = CLw+(St/Sw)*CLt; // CL
        CD = CDp0*(1f+Mathf.Abs(Mathf.Pow((alpha/9f),3f)))+((CL*CL)/(Mathf.PI*ew*AR))*CGE; // CD

        // Force
        Cx = CL*Mathf.Sin(Mathf.Deg2Rad*alpha)-CD*Mathf.Cos(Mathf.Deg2Rad*alpha); // Cx
        Cy = Cyb*beta+Cyp*(1f/Mathf.Rad2Deg)*((p*bw)/(2f*Airspeed))+Cyr*(1f/Mathf.Rad2Deg)*((r*bw)/(2f*Airspeed))+Cydr*dr; // Cy
        Cz = -CL*Mathf.Cos(Mathf.Deg2Rad*alpha)-CD*Mathf.Sin(Mathf.Deg2Rad*alpha); // Cz

        // Torque
        Cl = Clb*beta+Clp*(1f/Mathf.Rad2Deg)*((p*bw)/(2f*Airspeed))+Clr*(1f/Mathf.Rad2Deg)*((r*bw)/(2f*Airspeed))+Cldr*dr; // Cl
        Cm = Cmw0+CLw*hw-VH*CLt+CL*dh; // Cm       
        Cn = Cnb*beta+Cnp*(1f/Mathf.Rad2Deg)*((p*bw)/(2f*Airspeed))+Cnr*(1f/Mathf.Rad2Deg)*((r*bw)/(2f*Airspeed))+Cndr*dr; // Cn

        AerodynamicForce.x = 0.5f*rho*Airspeed*Airspeed*Sw*Cx;
        AerodynamicForce.y = 0.5f*rho*Airspeed*Airspeed*Sw*(-Cz);
        AerodynamicForce.z = 0.5f*rho*Airspeed*Airspeed*Sw*(-Cy);

        AerodynamicMomentum.x = 0.5f*rho*Airspeed*Airspeed*Sw*bw*(-Cl);//roll
        AerodynamicMomentum.y = 0.5f*rho*Airspeed*Airspeed*Sw*bw*Cn;//yaw
        AerodynamicMomentum.z = 0.5f*rho*Airspeed*Airspeed*Sw*cMAC*Cm;//pitch

        float Distance = (PlaneRigidbody.position-MyGameManeger.instance.PlatformPosition).magnitude-10f;
        if(MyGameManeger.instance.FlightMode=="BirdmanRally" && Distance<-0.5f){
            
            CalculateRotation();
            
            float W = PlaneRigidbody.mass*Physics.gravity.magnitude;//重力
            float L = 0.5f*rho*Airspeed*Airspeed*Sw*(Cx*Mathf.Sin(Mathf.Deg2Rad*theta)-Cz*Mathf.Cos(Mathf.Deg2Rad*theta));//揚力
            float N = (W-L)*Mathf.Cos(Mathf.Deg2Rad*3.5f); // N=(W-L)*cos(3.5deg)//翼持ちの抵抗力
            float P = (PlaneRigidbody.mass*MyGameManeger.instance.Airspeed_TO*MyGameManeger.instance.Airspeed_TO)/(2f*10f); // P=m*Vto*Vto/2*L//推進力
            
            //離陸方向をYaw回転に合わせて水平方向に修正
            //Vector3 takeoffDirection = Quaternion.Euler(0f, MyGameManeger.instance.StartRotation, 0f) * Vector3.forward;
            //TakeoffForce = takeoffDirection * P;

            //TakeoffForce.y = N*Mathf.Cos(Mathf.Deg2Rad*3.5f);

            //float TOFh = P;
            //float TOFv = N*Mathf.Cos(Mathf.Deg2Rad*3.5f);
            //TakeoffForce.x = TOFv*Mathf.Sin(MyGameManeger.instance.TailRotation) + TOFh*Mathf.Cos(MyGameManeger.instance.TailRotation);
            //TakeoffForce.y = TOFv*Mathf.Cos(MyGameManeger.instance.TailRotation) - TOFh*Mathf.Sin(MyGameManeger.instance.TailRotation);
            //Debug.Log("Power:"+P);
            
            TakeoffForce.x = P*Mathf.Cos(Mathf.Deg2Rad*MyGameManeger.instance.StartRotation);
            TakeoffForce.y = N*Mathf.Cos(Mathf.Deg2Rad*3.5f);
            TakeoffForce.z = -P*Mathf.Sin(Mathf.Deg2Rad*MyGameManeger.instance.StartRotation);
            
            AerodynamicForce.z = 0f;
            AerodynamicMomentum.x = 0f;//
            AerodynamicMomentum.y = 0f;

            transform.rotation = Quaternion.Euler(transform.localEulerAngles.x, transform.localEulerAngles.y, MyGameManeger.instance.TailRotation);
            //PlaneRigidbody.constraints = RigidbodyConstraints.FreezePositionZ;

            if(AerodynamicMomentum.x <= 0){//左から右に吹く風 左翼がより大きな揚力を生む
                if(Mathf.Abs(AerodynamicMomentum.x) > YL*YMin){//左翼が翼持ちの手を離れている状態
                    //Debug.Log("A1");
                    YlMoment = 0;//既に翼持ちを離れている為、翼持ちはモーメントを与えられない
                }
                else{
                    //Debug.Log("B1");
                    YlMoment = -YL*YMin - AerodynamicMomentum.x;//翼持ちが与えるモーメントは、機体を支える最小限のモーメントから風が与えるそれを引いた値である
                }

                if(Mathf.Abs(AerodynamicMomentum.x + YlMoment) <= YL*YrMax){//右翼持ちにまだ余裕がある状態
                    //Debug.Log("C1");
                    YrMoment = -(AerodynamicMomentum.x + YlMoment);//翼持ちに風と逆の翼持ちのモーメントを足した大きな負荷が掛かるが、まだ耐えられる
                }else{
                    //Debug.Log("D1");
                    YrMoment = YL*YrMax;//つり合いが取れずに右翼持ちのモーメントが足りない状態
                }

            }else{//右から左に吹く風 右翼がより大きな揚力を生む
                if(Mathf.Abs(AerodynamicMomentum.x) > YL*YMin){//右翼が翼持ちの手を離れている状態
                    //Debug.Log("A2");
                    YrMoment = 0;
                }
                else{
                    //Debug.Log("B2");
                    YrMoment = YL*YMin - AerodynamicMomentum.x;
                }

                if(Mathf.Abs(AerodynamicMomentum.x + YrMoment) <= YL*YlMax){//左翼持ちにまだ余裕がある状態
                    //Debug.Log("C2");
                    YlMoment = AerodynamicMomentum.x + YrMoment;
                }else{
                    //Debug.Log("D2");
                    YlMoment = YL*YlMax;
                }
                
            }
            //Debug.Log("YlMoment:"+YlMoment+"YrMoment:"+YrMoment+"aeroX:"+AerodynamicMomentum.x);
            //AerodynamicMomentum.x += YrMoment + YlMoment;//最終的なロールモーメントの計算//一旦消す
            MyGameManeger.instance.TakeOff = false;
        }
        else{
            MyGameManeger.instance.TakeOff = true;
            //PlaneRigidbody.constraints = RigidbodyConstraints.None;
        }
        //else if(MyGameManeger.instance.FlightMode=="BirdmanRally" && !AddTaleForce){
        //    AddTaleForce =true;
        //}
        //Debug.Log(AerodynamicForce.z);
        PlaneRigidbody.AddRelativeForce(AerodynamicForce, ForceMode.Force);
        PlaneRigidbody.AddRelativeTorque(AerodynamicMomentum, ForceMode.Force);
        PlaneRigidbody.AddForce(TakeoffForce, ForceMode.Force);
        nz = AerodynamicForce.y/(PlaneRigidbody.mass*Physics.gravity.magnitude);
    }

    void CalculateRotation()
    {
        float q1 = MyGameManeger.instance.Plane.transform.rotation.x;
        float q2 = -MyGameManeger.instance.Plane.transform.rotation.y;
        float q3 = -MyGameManeger.instance.Plane.transform.rotation.z;
        float q4 = MyGameManeger.instance.Plane.transform.rotation.w;
        float C11 = q1*q1-q2*q2-q3*q3+q4*q4;
        float C22 = -q1*q1+q2*q2-q3*q3+q4*q4;
        float C12 = 2f*(q1*q2+q3*q4);
        float C13 = 2f*(q1*q3-q2*q4);
        float C32 = 2f*(q2*q3-q1*q4);

        phi = -Mathf.Atan(-C32/C22)*Mathf.Rad2Deg;
        theta = -Mathf.Asin(C12)*Mathf.Rad2Deg; 
        psi = -Mathf.Atan(-C13/C11)*Mathf.Rad2Deg;
    }

    void InputSpecifications()
    {
            if(MyGameManeger.instance.PlaneName == "QX-18"){
            // Plane
            PlaneRigidbody.mass = 93.875f; // [kg]
            PlaneRigidbody.centerOfMass = new Vector3(0f,0.221f,0f); // [m]
            PlaneRigidbody.inertiaTensor = new Vector3(876f,947f,76f);
            PlaneRigidbody.inertiaTensorRotation = Quaternion.AngleAxis(-4.833f, Vector3.forward);
            // Specification At Cruise without Ground Effect
            Airspeed0 = 9.700f; // Magnitude of ground speed [m/s]
            alpha0 = 1.682f; // Angle of attack [deg]
            CDp0 = 0.018f; // Parasitic drag [-]
            Cmw0 = -0.164f; // Pitching momentum [-]
            CLMAX = 1.700f;
            // Wing
            Sw = 18.042f; // Wing area of wing [m^2]
            bw = 25.133f; // Wing span [m]
            cMAC = 0.757f; // Mean aerodynamic chord [m]
            aw = 0.108f; // Wing Lift Slope [1/deg]
            hw = (0.323f-0.250f); // Length between Wing a.c. and c.g.
            ew = 0.949f; // Wing efficiency
            AR = (bw*bw)/Sw; // Aspect Ratio
            // Tail
            Downwash = true; // Conventional Tail: True, T-Tail: False
            St = 1.375f; // Wing area of tail
            at = 0.076f; // Tail Lift Slope [1/deg]
            lt = 4.200f; // Length between Tail a.c. and c.g.
            deMAX = 10.000f; // Maximum elevator angle
            tau = 1.000f; // Control surface angle of attack effectiveness [-]
            VH = (St*lt)/(Sw*cMAC); // Tail Volume
            // Fin
            drMAX = 10.000f; // Maximum rudder angle            
            // Ground Effect
            CGEMIN = 0.215f; // Minimum Ground Effect Coefficient [-]
            // Stability derivatives
            Cyb = -0.002410f; // [1/deg]
            Cyp = -0.228437f; // [1/rad]
            Cyr = 0.090542f; // [1/rad]
            Cydr = 0.001908f; // [1/deg]
            Clb = -0.002002f; // [1/deg]
            Clp = -0.877559f; // [1/rad]
            Clr = 0.237651f; // [1/rad]
            Cldr = 0.000052f; // [1/deg]
            Cnb = -0.000059f; // [1/deg]
            Cnp = -0.142441f; // [1/rad]
            Cnr = -0.000491f; // [1/rad]
            Cndr = -0.000262f; // [1/deg]
        }else if(MyGameManeger.instance.PlaneName == "Tatsumi"){
            // Plane//と見せかけた竜海です
            PlaneRigidbody.mass = 100f;
            PlaneRigidbody.centerOfMass = new Vector3(0f,0.053f,0f);
            PlaneRigidbody.inertiaTensor = new Vector3(993.022f,1028.254f,50.789f);
            PlaneRigidbody.inertiaTensorRotation = Quaternion.AngleAxis(-9.134f, Vector3.forward);
            // Specification At Cruise without Ground Effect
            Airspeed0 = 10.500f; // Magnitude of ground speed [m/s]
            alpha0 = 0f; // Angle of attack [deg]
            CDp0 = 0.007f; // Parasitic drag [-]
            Cmw0 = -0.132f; // Pitching momentum [-]
            CLMAX = 1.700f;
            // Wing
            Sw = 18.989f; // Wing area of wing [m^2]
            bw = 24.975f; // Wing span [m]
            cMAC = 0.812f; // Mean aerodynamic chord [m]
            aw = 0.103f; // Wing Lift Slope [1/deg]
            
            ac = 0.350f;//空力中心[MAC]
            cg = 0.250f;//初期重心中心[MAC]
            
            hw = ac-cg; // Length between Wing a.c. and c.g.
            ew = 0.987f; // Wing efficiency
            AR = 32.848f; // Aspect Ratio
            // Tail
            Downwash = true; // Conventional Tail: True, T-Tail: False
            St = 1.769f; // Wing area of tail
            at = 0.082f; // Tail Lift Slope [1/deg]
            lt = 2.900f; // Length between Tail a.c. and c.g.
            deMAX = 10.000f; // Maximum elevator angle
            tau = 1.000f; // Control surface angle of attack effectiveness [-]
            VH = 0.333f; // Tail Volume
            // Fin
            drMAX = 20.000f; // Maximum rudder angle            
            // Ground Effect
            CGEMIN = 0.230f; // Minimum Ground Effect Coefficient [-]
            // Stability derivatives
            Cyb = -0.004943f; // [1/deg]
            Cyp = -0.539872f; // [1/rad]
            Cyr =  0.161102f; // [1/rad]
            Cydr = 0.001612f; // [1/deg]
            Clb = -0.004711f; // [1/deg]
            Clp = -0.794525f; // [1/rad]
            Clr =  0.203056f; // [1/rad]
            Cldr = 0.000032f; // [1/deg]
            Cnb = -0.000364f; // [1/deg]
            Cnp = -0.100751f; // [1/rad]
            Cnr = -0.005821f; // [1/rad]
            Cndr = -0.000226f; // [1/deg]

            //追加機体データ
            lengthForward = 0.61f;
            lengthBackward = 0.47f;

            aircraftCenterOfMass = -0.225f;//機体のみ全重心(パイロットなし,ピッチのみ)[m]
            aircraftMass = 48.0f;//機体のみ全重量[kg]
            pilotMass = PlaneRigidbody.mass - aircraftMass;//パイロット体重[kg]

            YL = 2.8f;//機体中心から翼持ち棒までの長さ[m]
        }else if(MyGameManeger.instance.PlaneName == "QX-20"){
            // Plane
            PlaneRigidbody.mass = 98.797f;
            PlaneRigidbody.centerOfMass = new Vector3(0f,0.29f,0f);
            PlaneRigidbody.inertiaTensor = new Vector3(1003f,1045f,58f);
            PlaneRigidbody.inertiaTensorRotation = Quaternion.AngleAxis(-9.112f, Vector3.forward);
            // Specification At Cruise without Ground Effect
            Airspeed0 = 9.600f; // Magnitude of ground speed [m/s]
            alpha0 = 1.459f; // Angle of attack [deg]
            CDp0 = 0.016f; // Parasitic drag [-]
            Cmw0 =-0.114f; // Pitching momentum [-]
            CLMAX = 1.700f;
            // Wing
            Sw = 18.816f; // Wing area of wing [m^2]
            bw = 26.679f; // Wing span [m]
            cMAC = 0.755f; // Mean aerodynamic chord [m]
            aw = 0.108f; // Wing Lift Slope [1/deg]
            hw = (0.323f-0.250f); // Length between Wing a.c. and c.g.
            ew = 0.986f; // Wing efficiency
            AR = (bw*bw)/Sw; // Aspect Ratio
            // Tail
            Downwash = false; // Conventional Tail: True, T-Tail: False
            St = 1.526f; // Wing area of tail
            at = 0.088f; // Tail Lift Slope [1/deg]
            lt = 3.200f; // Length between Tail a.c. and c.g.
            deMAX = 10.000f; // Maximum elevator angle
            tau = 1.000f; // Control surface angle of attack effectiveness [-]
            VH = (St*lt)/(Sw*cMAC); // Tail Volume
            // Fin
            drMAX = 15.000f; // Maximum rudder angle            
            // Ground Effect
            CGEMIN = 0.293f; // Minimum Ground Effect Coefficient [-]
            // Stability derivatives
            Cyb = -0.003555f; // [1/deg]
            Cyp = -0.455493f; // [1/rad]
            Cyr = 0.143466f; // [1/rad]
            Cydr = 0.000888f; // [1/deg]
            Clb = -0.004049f; // [1/deg]
            Clp = -0.829690f; // [1/rad]
            Clr = 0.227736f; // [1/rad]
            Cldr = 0.000016f; // [1/deg]
            Cnb = -0.000500f; // [1/deg]
            Cnp = -0.132307f; // [1/rad]
            Cnr = 0.000942f; // [1/rad]
            Cndr = -0.000106f; // [1/deg]
        }else if(MyGameManeger.instance.PlaneName == "ARG-2"){
            // Plane
            PlaneRigidbody.mass = 103.100f;
            PlaneRigidbody.centerOfMass = new Vector3(0f,-0.019f,0f);
            PlaneRigidbody.inertiaTensor = new Vector3(961f,1024f,80f); //Ixx, Izz, Iyy
            PlaneRigidbody.inertiaTensorRotation = Quaternion.AngleAxis(-3.929f, Vector3.forward);
            // Specification At Cruise without Ground Effect
            Airspeed0 = 10.500f; // Magnitude of ground speed [m/s]
            alpha0 = 1.407f; // Angle of attack [deg] 
            CDp0 = 0.014f; // Parasitic drag [-]
            Cmw0 = -0.165f; // Pitching momentum [-]
            CLMAX = 1.700f;
            // Wing
            Sw = 18.009f; // Wing area of wing [m^2]
            bw = 23.350f; // Wing span [m]
            cMAC = 0.813f; // Mean aerodynamic chord [m]
            aw = 0.103f; // Wing Lift Slope [1/deg]
            hw = (0.3375f-0.250f); // Length between Wing a.c. and c.g.
            ew = 0.986f; // Wing efficiency
            AR = (bw*bw)/Sw; // Aspect Ratio
            // Tail
            Downwash = true; // Conventional Tail: True, T-Tail: False
            St = 1.651f; // Wing area of tail
            at = 0.074f; // Tail Lift Slope [1/deg]
            lt = 3.200f; // Length between Tail a.c. and c.g.
            deMAX = 10.000f; // Maximum elevator angle
            tau = 1.000f; // Control surface angle of attack effectiveness [-]
            VH = (St*lt)/(Sw*cMAC); // Tail Volume
            // Fin
            drMAX = 15.000f; // Maximum rudder angle            
            // Ground Effect
            CGEMIN = 0.215f; // Minimum Ground Effect Coefficient [-]
            // Stability derivatives
            Cyb = -0.003764f; // [1/deg]
            Cyp = -0.411848f; // [1/rad]
            Cyr =  0.141631f; // [1/rad]
            Cydr = 0.001846f; // [1/deg]
            Clb = -0.003656f; // [1/deg]
            Clp = -0.816226f; // [1/rad]
            Clr =  0.219104f; // [1/rad]
            Cldr = 0.000032f; // [1/deg]
            Cnb = -0.000245f; // [1/deg]
            Cnp = -0.127263f; // [1/rad]
            Cnr = -0.002745f; // [1/rad]
            Cndr = -0.000308f; // [1/deg]
        }else if(MyGameManeger.instance.PlaneName == "UL01B"){
            // Plane
            PlaneRigidbody.mass = 87.000f;
            PlaneRigidbody.centerOfMass = new Vector3(0f,0.290f,0f);
            PlaneRigidbody.inertiaTensor = new Vector3(886f,975f,113f); //Ixx, Izz, Iyy
            PlaneRigidbody.inertiaTensorRotation = Quaternion.AngleAxis(-5.581f, Vector3.forward);
            // Specification At Cruise without Ground Effect
            Airspeed0 = 8.500f; // Magnitude of ground speed [m/s]
            alpha0 = 1.521f; // Angle of attack [deg] 
            CDp0 = 0.015f; // Parasitic drag [-]
            Cmw0 = -0.122f; // Pitching momentum [-]
            CLMAX = 1.700f;
            // Wing
            Sw = 21.873f; // Wing area of wing [m^2]
            bw = 25.200f; // Wing span [m]
            cMAC = 0.911f; // Mean aerodynamic chord [m]
            aw = 0.103f; // Wing Lift Slope [1/deg]
            hw = (0.330f-0.250f); // Length between Wing a.c. and c.g.
            ew = 0.986f; // Wing efficiency
            AR = (bw*bw)/Sw; // Aspect Ratio
            // Tail
            Downwash = true; // Conventional Tail: True, T-Tail: False
            St = 2.160f; // Wing area of tail
            at = 0.074f; // Tail Lift Slope [1/deg]
            lt = 4.500f; // Length between Tail a.c. and c.g.
            deMAX = 12.000f; // Maximum elevator angle
            tau = 1.000f; // Control surface angle of attack effectiveness [-]
            VH = (St*lt)/(Sw*cMAC); // Tail Volume
            // Fin
            drMAX = 18.000f; // Maximum rudder angle            
            // Ground Effect
            CGEMIN = 0.360f; // Minimum Ground Effect Coefficient [-]
            // Stability derivatives
            Cyb = -0.010768f; // [1/deg]
            Cyp = -0.642834f; // [1/rad]
            Cyr =  0.320123f; // [1/rad]
            Cydr = 0.003872f; // [1/deg]
            Clb = -0.006073f; // [1/deg]
            Clp = -0.776507f; // [1/rad]
            Clr =  0.249355f; // [1/rad]
            Cldr = 0.000061f; // [1/deg]
            Cnb = -0.000336f; // [1/deg]
            Cnp = -0.135587f; // [1/rad]
            Cnr = -0.016244f; // [1/rad]
            Cndr = -0.000817f; // [1/deg]
        }else if(MyGameManeger.instance.PlaneName == "ORCA18"){
            // Plane
            PlaneRigidbody.mass = 96.000f;
            PlaneRigidbody.centerOfMass = new Vector3(0f,0.009f,0f);
            PlaneRigidbody.inertiaTensor = new Vector3(858f,949f,107f); //Ixx, Izz, Iyy
            PlaneRigidbody.inertiaTensorRotation = Quaternion.AngleAxis(-2.972f, Vector3.forward);
            // Specification At Cruise without Ground Effect
            Airspeed0 = 8.000f; // Magnitude of ground speed [m/s]
            alpha0 = 1.500f; // Angle of attack [deg] 
            CDp0 = 0.015f; // Parasitic drag [-]
            Cmw0 = -0.108f; // Pitching momentum [-]
            CLMAX = 1.700f;
            // Wing
            Sw = 20.257f; // Wing area of wing [m^2]
            bw = 24.100f; // Wing span [m]
            cMAC = 0.900f; // Mean aerodynamic chord [m]
            aw = 0.103f; // Wing Lift Slope [1/deg]
            hw = (0.329f-0.250f); // Length between Wing a.c. and c.g.
            ew = 0.986f; // Wing efficiency
            AR = (bw*bw)/Sw; // Aspect Ratio
            // Tail
            Downwash = true; // Conventional Tail: True, T-Tail: False
            St = 1.950f; // Wing area of tail
            at = 0.077f; // Tail Lift Slope [1/deg]
            lt = 3.900f; // Length between Tail a.c. and c.g.
            deMAX = 12.000f; // Maximum elevator angle
            tau = 1.000f; // Control surface angle of attack effectiveness [-]
            VH = (St*lt)/(Sw*cMAC); // Tail Volume
            // Fin
            drMAX = 20.000f; // Maximum rudder angle            
            // Ground Effect
            CGEMIN = 0.290f; // Minimum Ground Effect Coefficient [-]
            // Stability derivatives
            Cyb = -0.003716f; // [1/deg]
            Cyp = -0.375654f; // [1/rad]
            Cyr =  0.187645f; // [1/rad]
            Cydr = 0.001276f; // [1/deg]
            Clb = -0.003325f; // [1/deg]
            Clp = -0.792170f; // [1/rad]
            Clr =  0.302277f; // [1/rad]
            Cldr = 0.000000f; // [1/deg]
            Cnb = -0.000324f; // [1/deg]
            Cnp = -0.169856f; // [1/rad]
            Cnr = -0.003154f; // [1/rad]
            Cndr = -0.000233f; // [1/deg]
        }else if(MyGameManeger.instance.PlaneName == "ORCA22"){
            // Plane
            PlaneRigidbody.mass = 95.000f;
            PlaneRigidbody.centerOfMass = new Vector3(0f,0.014f,0f);
            PlaneRigidbody.inertiaTensor = new Vector3(904f,1004f,113f); //Ixx, Izz, Iyy
            PlaneRigidbody.inertiaTensorRotation = Quaternion.AngleAxis(-3.015f, Vector3.forward);
            // Specification At Cruise without Ground Effect
            Airspeed0 = 8.400f; // Magnitude of ground speed [m/s]
            alpha0 = 1.395f; // Angle of attack [deg] 
            CDp0 = 0.016f; // Parasitic drag [-]
            Cmw0 = -0.105f; // Pitching momentum [-]
            CLMAX = 1.700f;
            // Wing
            Sw = 18.560f; // Wing area of wing [m^2]
            bw = 25.400f; // Wing span [m]
            cMAC = 0.797f; // Mean aerodynamic chord [m]
            aw = 0.104f; // Wing Lift Slope [1/deg]
            hw = (0.329f-0.250f); // Length between Wing a.c. and c.g.
            ew = 0.986f; // Wing efficiency
            AR = (bw*bw)/Sw; // Aspect Ratio
            // Tail
            Downwash = true; // Conventional Tail: True, T-Tail: False
            St = 1.392f; // Wing area of tail
            at = 0.074f; // Tail Lift Slope [1/deg]
            lt = 3.900f; // Length between Tail a.c. and c.g.
            deMAX = 12.000f; // Maximum elevator angle
            tau = 1.000f; // Control surface angle of attack effectiveness [-]
            VH = (St*lt)/(Sw*cMAC); // Tail Volume
            // Fin
            drMAX = 20.000f; // Maximum rudder angle            
            // Ground Effect
            CGEMIN = 0.290f; // Minimum Ground Effect Coefficient [-]
            // Stability derivatives
            Cyb = -0.003515f; // [1/deg]
            Cyp = -0.307586f; // [1/rad]
            Cyr =  0.155767f; // [1/rad]
            Cydr = 0.001392f; // [1/deg]
            Clb = -0.002719f; // [1/deg]
            Clp = -0.756397f; // [1/rad]
            Clr =  0.274225f; // [1/rad]
            Cldr = 0.000000f; // [1/deg]
            Cnb = -0.000148f; // [1/deg]
            Cnp = -0.155515f; // [1/rad]
            Cnr = -0.003774f; // [1/rad]
            Cndr = -0.000241f; // [1/deg]
        }else if(MyGameManeger.instance.PlaneName == "Gardenia"){
            // Plane
            PlaneRigidbody.mass = 104.700f;
            PlaneRigidbody.centerOfMass = new Vector3(0f,0.011f,0f);
            PlaneRigidbody.inertiaTensor = new Vector3(1118f,1161f,63.790f); //Ixx, Izz, Iyy
            PlaneRigidbody.inertiaTensorRotation = Quaternion.AngleAxis(-6.083f, Vector3.forward);
            // Specification At Cruise without Ground Effect
            Airspeed0 = 10.300f; // Magnitude of ground speed [m/s]
            alpha0 = 1.378f; // Angle of attack [deg] 
            CDp0 = 0.014f; // Parasitic drag [-]
            Cmw0 = -0.150f; // Pitching momentum [-]
            CLMAX = 1.700f;
            // Wing
            Sw = 18.592f; // Wing area of wing [m^2]
            bw = 25.833f; // Wing span [m]
            cMAC = 0.758f; // Mean aerodynamic chord [m]
            aw = 0.104f; // Wing Lift Slope [1/deg]
            hw = (0.350f-0.250f); // Length between Wing a.c. and c.g.
            ew = 0.986f; // Wing efficiency
            AR = (bw*bw)/Sw; // Aspect Ratio
            // Tail
            Downwash = true; // Conventional Tail: True, T-Tail: False
            St = 1.604f; // Wing area of tail
            at = 0.084f; // Tail Lift Slope [1/deg]
            lt = 3.030f; // Length between Tail a.c. and c.g.
            deMAX = 10.000f; // Maximum elevator angle
            tau = 1.000f; // Control surface angle of attack effectiveness [-]
            VH = (St*lt)/(Sw*cMAC); // Tail Volume
            // Fin
            drMAX = 10.000f; // Maximum rudder angle            
            // Ground Effect
            CGEMIN = 0.210f; // Minimum Ground Effect Coefficient [-]
            // Stability derivatives
            Cyb = -0.003350f; // [1/deg]
            Cyp = -0.323739f; // [1/rad]
            Cyr =  0.125542f; // [1/rad]
            Cydr = 0.002195f; // [1/deg]
            Clb = -0.002857f; // [1/deg]
            Clp = -0.827828f; // [1/rad]
            Clr =  0.236597f; // [1/rad]
            Cldr = 0.000042f; // [1/deg]
            Cnb = -0.000158f; // [1/deg]
            Cnp = -0.136255f; // [1/rad]
            Cnr = -0.001478f; // [1/rad]
            Cndr = -0.000306f; // [1/deg]
        }else if(MyGameManeger.instance.PlaneName == "Aria"){
            // Plane
            PlaneRigidbody.mass = 122.000f;
            PlaneRigidbody.centerOfMass = new Vector3(0f,0.007f,0f);
            PlaneRigidbody.inertiaTensor = new Vector3(1646f,1698f,67f); //Ixx, Izz, Iyy
            PlaneRigidbody.inertiaTensorRotation = Quaternion.AngleAxis(-5.487f, Vector3.forward);
            // Specification At Cruise without Ground Effect
            Airspeed0 = 10.300f; // Magnitude of ground speed [m/s]
            alpha0 = 1.225f; // Angle of attack [deg] 
            CDp0 = 0.013f; // Parasitic drag [-]
            Cmw0 = -0.133f; // Pitching momentum [-]
            CLMAX = 1.700f;
            // Wing
            Sw = 20.754f; // Wing area of wing [m^2]
            bw = 29.021f; // Wing span [m]
            cMAC = 0.808f; // Mean aerodynamic chord [m]
            aw = 0.105f; // Wing Lift Slope [1/deg]
            hw = (0.350f-0.250f); // Length between Wing a.c. and c.g.
            ew = 0.986f; // Wing efficiency
            AR = (bw*bw)/Sw; // Aspect Ratio
            // Tail
            Downwash = true; // Conventional Tail: True, T-Tail: False
            St = 1.832f; // Wing area of tail
            at = 0.083f; // Tail Lift Slope [1/deg]
            lt = 3.030f; // Length between Tail a.c. and c.g.
            deMAX = 10.000f; // Maximum elevator angle
            tau = 1.000f; // Control surface angle of attack effectiveness [-]
            VH = (St*lt)/(Sw*cMAC); // Tail Volume
            // Fin
            drMAX = 10.000f; // Maximum rudder angle            
            // Ground Effect
            CGEMIN = 0.210f; // Minimum Ground Effect Coefficient [-]
            // Stability derivatives
            Cyb = -0.003069f; // [1/deg]
            Cyp = -0.228176f; // [1/rad]
            Cyr =  0.095274f; // [1/rad]
            Cydr = 0.002427f; // [1/deg]
            Clb = -0.002005f; // [1/deg]
            Clp = -0.741574f; // [1/rad]
            Clr =  0.206900f; // [1/rad]
            Cldr = 0.000042f; // [1/deg]
            Cnb = -0.000012f; // [1/deg]
            Cnp = -0.120109f; // [1/rad]
            Cnr = -0.001328f; // [1/rad]
            Cndr = -0.000301f; // [1/deg]
        }else if(MyGameManeger.instance.PlaneName == "Camellia"){
            // Plane
            PlaneRigidbody.mass = 109.800f;
            PlaneRigidbody.centerOfMass = new Vector3(0f,0.001f,0f);
            PlaneRigidbody.inertiaTensor = new Vector3(1486.608f,1529.392f,59.860f); //Ixx, Izz, Iyy
            PlaneRigidbody.inertiaTensorRotation = Quaternion.AngleAxis(-5.492f, Vector3.forward);
            // Specification At Cruise without Ground Effect
            Airspeed0 = 10.400f; // Magnitude of ground speed [m/s]
            alpha0 = 1.355f; // Angle of attack [deg] 
            CDp0 = 0.013f; // Parasitic drag [-]
            Cmw0 = -0.128f; // Pitching momentum [-]
            CLMAX = 1.700f;
            // Wing
            Sw = 19.653f; // Wing area of wing [m^2]
            bw = 25.697f; // Wing span [m]
            cMAC = 0.795f; // Mean aerodynamic chord [m]
            aw = 0.104f; // Wing Lift Slope [1/deg]
            hw = (0.350f-0.250f); // Length between Wing a.c. and c.g.
            ew = 0.986f; // Wing efficiency
            AR = (bw*bw)/Sw; // Aspect Ratio
            // Tail
            Downwash = true; // Conventional Tail: True, T-Tail: False
            St = 1.719f; // Wing area of tail
            at = 0.083f; // Tail Lift Slope [1/deg]
            lt = 3.030f; // Length between Tail a.c. and c.g.
            deMAX = 10.000f; // Maximum elevator angle
            tau = 1.000f; // Control surface angle of attack effectiveness [-]
            VH = (St*lt)/(Sw*cMAC); // Tail Volume
            // Fin
            drMAX = 10.000f; // Maximum rudder angle            
            // Ground Effect
            CGEMIN = 0.210f; // Minimum Ground Effect Coefficient [-]
            // Stability derivatives
            Cyb = -0.003104f; // [1/deg]
            Cyp = -0.317546f; // [1/rad]
            Cyr =  0.112886f; // [1/rad]
            Cydr = 0.002013f; // [1/deg]
            Clb = -0.002799f; // [1/deg]
            Clp = -0.850998f; // [1/rad]
            Clr =  0.224309f; // [1/rad]
            Cldr = 0.000039f; // [1/deg]
            Cnb = -0.000132f; // [1/deg]
            Cnp = -0.129671f; // [1/rad]
            Cnr = -0.001558f; // [1/rad]
            Cndr = -0.000282f; // [1/deg]
        }
        else if (MyGameManeger.instance.PlaneName == "Mio")
        {
            // Plane
            PlaneRigidbody.mass = 95.351f;
            PlaneRigidbody.centerOfMass = new Vector3(0.3662f, -0.7296f, 0);//バランスボードが無いとき用の設定
            PlaneRigidbody.inertiaTensor = new Vector3(1062.324f, 1111.022f, 62.217f); //Ixx, Izz, Iyy);
            PlaneRigidbody.inertiaTensorRotation = Quaternion.AngleAxis(-3.710f, Vector3.forward);
            // Specification At Cruise without Ground Effect
            Airspeed0 = 10.200f; // Magnitude of ground speed [m/s]
            alpha0 = 0.000f; // Angle of attack [deg] 
            CDp0 = 0.008f; // Parasitic drag [-]
            Cmw0 = -0.126f; // Pitching momentum [-]
            CLMAX = 1.700f;
            // Wing
            Sw = 18.659f; // Wing area of wing [m^2]
            bw = 25.285f; // Wing span [m]
            cMAC = 0.773f; // Mean aerodynamic chord [m]
            aw = 0.104f; // Wing Lift Slope [1/deg]
            
            ac = 0.350f;//空力中心[MAC]
            cg = 0.250f;//初期重心中心[MAC]
            
            hw = ac-cg; // Length between Wing a.c. and c.g.
            ew = 0.986f; // Wing efficiency
            AR = (bw*bw)/Sw; // Aspect Ratio
            // Tail
            Downwash = true; // Conventional Tail: True, T-Tail: False
            St = 1.650f; // Wing area of tail
            at = 0.080f; // Tail Lift Slope [1/deg]
            lt = 2.900f; // Length between Tail a.c. and c.g.
            deMAX = 10.000f; // Maximum elevator angle
            tau = 1.000f; // Control surface angle of attack effectiveness [-]
            VH = (St*lt)/(Sw*cMAC); // Tail Volume
            // Fin
            drMAX = 15.000f; // Maximum rudder angle            
            // Ground Effect
            CGEMIN = 0.230f; // Minimum Ground Effect Coefficient [-]
            // Stability derivatives
            Cyb = -0.004855f; // [1/deg]
            Cyp = -0.446976f; // [1/rad]
            Cyr = 0.152451f; // [1/rad]
            Cydr = 0.002537f; // [1/deg]
            Clb = -0.003901f; // [1/deg]
            Clp = -0.970776f; // [1/rad]
            Clr =  0.256819f; // [1/rad]
            Cldr = 0.000075f; // [1/deg]
            Cnb = -0.000153f; // [1/deg]
            Cnp = -0.126670f; // [1/rad]
            Cnr = -0.008080f; // [1/rad]
            Cndr = -0.000341f; // [1/deg]
        }
else if (MyGameManeger.instance.PlaneName == "Ray")
        {
            // Plane
            PlaneRigidbody.mass = 100.402f;
            PlaneRigidbody.centerOfMass = new Vector3(0f,-0.143f,0f);
            PlaneRigidbody.inertiaTensor = new Vector3(966f,1005f,60f);
            PlaneRigidbody.inertiaTensorRotation = Quaternion.AngleAxis(-4.570f, Vector3.forward);
            // Specification At Cruise without Ground Effect
            Airspeed0 = 10.500f; // Magnitude of ground speed [m/s]
            alpha0 = 0.000f; // Angle of attack [deg]
            CDp0 = 0.008f; // Parasitic drag [-]
            Cmw0 = -0.120f; // Pitching momentum [-]
            CLMAX = 1.700f;
            // Wing
            Sw = 18.171f; // Wing area of wing [m^2]
            bw = 24.400f; // Wing span [m]
            cMAC = 0.788f; // Mean aerodynamic chord [m]
            aw = 0.107f; // Wing Lift Slope [1/deg]

            ac = 0.350f;//空力中心[MAC]
            cg = 0.250f;//初期重心中心[MAC]
            hw = ac-cg; // Length between Wing a.c. and c.g.
            ew = 0.987f; // Wing efficiency
            AR = (bw*bw)/Sw; // Aspect Ratio
            // Tail
            Downwash = true; // Conventional Tail: True, T-Tail: False
            St = 1.841f; // Wing area of tail
            at = 0.089f; // Tail Lift Slope [1/deg]
            lt = 2.800f; // Length between Tail a.c. and c.g.
            deMAX = 10.000f; // Maximum elevator angle
            tau = 1.000f; // Control surface angle of attack effectiveness [-]
            VH = (St*lt)/(Sw*cMAC); // Tail Volume
            // Fin
            drMAX = 15.000f; // Maximum rudder angle            
            // Ground Effect
            CGEMIN = 0.215f; // Minimum Ground Effect Coefficient [-]
            // Stability derivatives
            Cyb = -0.003374f; // [1/deg]
            Cyp = -0.353566f; // [1/rad]
            Cyr = 0.120345f; // [1/rad]
            Cydr = 0.0020191f; // [1/deg]
            Clb = -0.003077f; // [1/deg]
            Clp = -0.792464f; // [1/rad]
            Clr = 0.198706f; // [1/rad]
            Cldr = 0.000012f; // [1/deg]
            Cnb = -0.000091f; // [1/deg]
            Cnp = -0.099047f; // [1/rad]
            Cnr = -0.006814f; // [1/rad]
            Cndr = -0.000290f; // [1/deg]
            //追加機体データ//注意！仮データ
            lengthForward = 0.9f+0.34f;//フレーム前方(フレーム＋センサー部分)から桁(原点)位置[m]
            lengthBackward = 0.5f;//フレーム後方(フレームの端)から桁(原点)位置[m]
            aircraftCenterOfMass = -0.25f;//機体のみ全重心(パイロットなし,ピッチのみ)[m]
            aircraftMass = 50;//機体のみ全重量[kg]
        }

        if(CanReadCsv){//CSVファイルが読み込まれた場合は優先的にそちらのデータを利用
            try{
            // Plane
            PlaneRigidbody.mass = float.Parse(CsvList[1][1]);
            PlaneRigidbody.centerOfMass = new Vector3(float.Parse(CsvList[2][1]),float.Parse(CsvList[2][2]),float.Parse(CsvList[2][3]));
            PlaneRigidbody.inertiaTensor = new Vector3(float.Parse(CsvList[3][1]),float.Parse(CsvList[3][2]),float.Parse(CsvList[3][3]));
            PlaneRigidbody.inertiaTensorRotation = Quaternion.AngleAxis(float.Parse(CsvList[4][1]), Vector3.forward);
            // Specification At Cruise without Ground Effect
            Airspeed0 = float.Parse(CsvList[6][1]); // Magnitude of ground speed [m/s]
            alpha0 = float.Parse(CsvList[7][1]); // Angle of attack [deg]
            CDp0 = float.Parse(CsvList[8][1]); // Parasitic drag [-]
            Cmw0 = float.Parse(CsvList[9][1]); // Pitching momentum [-]
            CLMAX = float.Parse(CsvList[10][1]);
            // Wing
            Sw = float.Parse(CsvList[12][1]); // Wing area of wing [m^2]
            bw = float.Parse(CsvList[13][1]); // Wing span [m]
            cMAC = float.Parse(CsvList[14][1]); // Mean aerodynamic chord [m]
            aw = float.Parse(CsvList[15][1]); // Wing Lift Slope [1/deg]

            hw = float.Parse(CsvList[16][1]); // Length between Wing a.c. and c.g.
            ew = float.Parse(CsvList[17][1]); // Wing efficiency
            AR = float.Parse(CsvList[18][1]); // Aspect Ratio
            // Tail
            Downwash = Convert.ToBoolean(CsvList[20][1]); // Conventional Tail: True, T-Tail: False
            St = float.Parse(CsvList[21][1]); // Wing area of tail
            at = float.Parse(CsvList[22][1]); // Tail Lift Slope [1/deg]
            lt = float.Parse(CsvList[23][1]); // Length between Tail a.c. and c.g.
            deMAX = float.Parse(CsvList[24][1]); // Maximum elevator angle
            tau = float.Parse(CsvList[25][1]); // Control surface angle of attack effectiveness [-]
            VH = float.Parse(CsvList[26][1]); // Tail Volume
            // Fin
            drMAX = float.Parse(CsvList[37][1]); // Maximum rudder angle            
            // Ground Effect
            CGEMIN = float.Parse(CsvList[2][6]); // Minimum Ground Effect Coefficient [-]
            // Stability derivatives
            Cyb = float.Parse(CsvList[5][6]); // [1/deg]
            Cyp = float.Parse(CsvList[6][6]); // [1/rad]
            Cyr = float.Parse(CsvList[7][6]); // [1/rad]
            Cydr = float.Parse(CsvList[8][6]); // [1/deg]
            Clb = float.Parse(CsvList[9][6]); // [1/deg]
            Clp = float.Parse(CsvList[10][6]); // [1/rad]
            Clr = float.Parse(CsvList[11][6]); // [1/rad]
            Cldr = float.Parse(CsvList[12][6]); // [1/deg]
            Cnb = float.Parse(CsvList[13][6]); // [1/deg]
            Cnp = float.Parse(CsvList[14][6]); // [1/rad]
            Cnr = float.Parse(CsvList[15][6]); // [1/rad]
            Cndr = float.Parse(CsvList[16][6]); // [1/deg]

            //追加機体データ
            lengthForward = float.Parse(CsvList[19][6]);//前センサーから吊り具(桁中心)までの長さ[m]
            lengthBackward = float.Parse(CsvList[20][6]);//吊り具(桁中心)から後センサーまでの長さ[m]

            aircraftCenterOfMass = float.Parse(CsvList[21][6]);;//機体のみ全重心(パイロットなし,ピッチのみ)[m]
            aircraftMass = float.Parse(CsvList[22][6]);;//機体のみ全重量[kg]
            pilotMass = PlaneRigidbody.mass - aircraftMass;//パイロット体重[kg]

            YL = float.Parse(CsvList[23][6]);;//機体中心から翼持ち棒までの長さ[m]

            MyGameManeger.instance.error = true;
            MyGameManeger.instance.errorText = "CSVファイル読み込み成功";

            }catch(Exception e){
                Debug.LogWarning("CSV file error"+e);
                MyGameManeger.instance.error = true;
                MyGameManeger.instance.errorText = "CSVファイルに不備があります。Dataフォルダ内のCSVファイルを確認するか、\n内蔵データを使用する為にそれを削除してください。";
            }
        }
    }
    void WriteFile(string txt) {
        FileInfo fi = new FileInfo(path);
        using (StreamWriter sw = fi.AppendText()) {
            sw.WriteLine(txt);
        }
    }

void ReadFile() {
    path = Application.dataPath + "/" + fileName;

    if (!File.Exists(path)) {
        Debug.LogWarning("CSV file not found at: " + path);
        return;
    }
    FileInfo fi = new FileInfo(path);
    try {
        using (StreamReader sr = new StreamReader(fi.OpenRead(), Encoding.UTF8)) {
            string readTxt = sr.ReadToEnd();
            //Debug.Log("Read CSV content:\n" + readTxt);

            CsvList.Clear(); // データリストの初期化

            string[] lines = readTxt.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            foreach (string line in lines) {
                //Debug.Log($"Parsed line: '{line}' (Length: {line.Length})");
                string[] values = line.Split(new[] { ',' }, StringSplitOptions.None);

                foreach (string value in values) {
                    //Debug.Log($"Parsed value: '{value}' (Length: {value.Length})");
                }

                CsvList.Add(new List<string>(values));
            }
            CanReadCsv = true;
            /*//デバッグ用
            for(int ii=0;ii<10;ii++){
                for(int jj=0;jj<38;jj++){
                    Debug.Log(CsvList[jj][ii]);
                }
            }
            */
        }
    } catch (Exception e) {
        Debug.LogWarning("Error reading CSV: " + e);
    }
}

public virtual void FlightModelStart(){}

public virtual void FlightModelFixedUpdate(){}

}
