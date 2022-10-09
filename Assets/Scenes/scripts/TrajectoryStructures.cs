using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ComStructures;
using TinyJson;
using System.IO;
using System.Threading.Tasks;

namespace TrajectoryStructures
{
    [System.Serializable]
    public struct keypoint
    {
        public CommonInfo cminfo;
        public PolarInfo pinfo;
        public CartesianInfo cinfo;
//        public float time;
        [ReadOnlyInspector]
        public bool set; // weither or not it has been initialized. Not really used. May be used for some checks
    }

    public struct EulerAngles
    {
        public float yaw, pitch, roll;
        public EulerAngles(float lyaw, float lpitch, float lroll)
        {
            yaw = lyaw;
            pitch = lpitch;
            roll = lroll;
        }
    }

    public struct trajectory
    {
        public AllConfig allcfg;
        public List<keypoint> kp_list; // = new List<keypoint>();
        /* autant accéder directement à la liste pour ces fonctions
                public void Add(keypoint kp)
                {
                    kp_list.Add(kp);
                }

                public void Insert(int pos, keypoint kp)
                {
                    kp_list.Insert(pos, kp);
                }
                float findTimeByKpIdx(int idx)
                {
                    return kp_list[idx].cminfo.time;
                }
                public void updateKp(int idx, keypoint kp)
                {
                    kp_list[idx] = kp;
                }
        */

        public int findKpByTime(float time)
        {
            int Count = kp_list.Count;

            if (Count == 0)
                return -1;
            if (time < kp_list[0].cminfo.time)
                return -1;
            if (time > kp_list[Count - 1].cminfo.time)
                return -1;

            // find kpa.cminfo.time<=time<kpb.cminfo.time
            int i;
            float t1, t2;

            for (i = 0; i < Count - 1; i++)
            {
                t1 = kp_list[i].cminfo.time;
                t2 = kp_list[i + 1].cminfo.time;

                if ((t1 <= time) && (time < t2))
                {
                    return i;
                }
            }

            if (time == kp_list[Count - 1].cminfo.time)
                return Count - 1;

            return -1;
        }


        public bool InsertAtTime(keypoint kp) // potentially replace if already exists at this time
        {
            int Count = kp_list.Count;
            float time = kp.cminfo.time;

            if (!kp.set)
                return false;

            if (Count == 0)
            {
                kp_list.Add(kp);
                return true;
            }

            if (time < kp_list[0].cminfo.time)
            {
                kp_list.Insert(0, kp);
                return true;
            }
            if (time > kp_list[Count-1].cminfo.time)
            {
                kp_list.Add(kp);
                return true;
            }

            if (Count == 1)
            {
                if (kp_list[0].cminfo.time == time)
                {
                    kp_list[0] = kp;
                    return true;
                }
                else // already processed
                {
                    Debug.Log("internal error 1");
                    return false;
                }
            }

            int i = findKpByTime(time);

            if (i == -1)
            {
                Debug.Log("internal error 2");
                return false; // should never happen
            }

            if (kp_list[i].cminfo.time == time)
                kp_list[i] = kp; // replace
            else
            kp_list.Insert(i+1,kp);

            return true;
        }

        public static Vector3 limitAngles(Vector3 ea)
        {
            if (ea[1] < -180)
                ea[1] += 360;
            if (ea[1] > 180)
                ea[1] -= 360;

            if (ea[0] < -180)
                ea[0] += 360;
            if (ea[0] > 180)
                ea[0] -= 360;

            if (ea[2] < -180)
                ea[2] += 360;
            if (ea[2] > 180)
                ea[2] -= 360;
            return ea;
        }

        EulerAngles EulerInterpol(EulerAngles ea1, EulerAngles ea2, float alpha)
        {
            EulerAngles ea = new EulerAngles();

            Quaternion rotationFrom = Quaternion.Euler(-ea1.pitch,-ea1.yaw,-ea1.roll);
            Quaternion rotationTo = Quaternion.Euler(-ea2.pitch, -ea2.yaw, -ea2.roll);

            Vector3 euler =  Quaternion.Slerp(rotationFrom, rotationTo, alpha).eulerAngles;

            euler = limitAngles(euler);

            ea.pitch = -euler[0];
            ea.yaw = -euler[1];
            ea.roll = -euler[2];

            return ea;
        }

        float lerp(float a, float b, float alpha)
        {
            return a + (b - a) * alpha;
        }


        public keypoint interpolate(float time)
        {
            int Count = kp_list.Count;
            int i;
            float t1, t2;

            keypoint kp = new keypoint();

            if (kp_list.Count == 0)
                return kp;

            if (time <= kp_list[0].cminfo.time)
                return kp_list[0];

            if (time >= kp_list[Count-1].cminfo.time)
                return kp_list[Count - 1];

            i = findKpByTime(time);

            if (i == -1)
                return kp;


            // then interpolate linearly
            {

                keypoint k1 = kp_list[i];
                keypoint k2 = kp_list[i + 1];

                t1 = k1.cminfo.time;
                t2 = k2.cminfo.time;

                if (t1 == t2)
                    return k1; // could be considered as an error

                float alpha = (time -t1) / (t2 - t1); // in [0..1]
                EulerAngles ea, ea1, ea2;

                // cminfo.
                kp.cminfo.alpha = lerp(k1.cminfo.alpha,k2.cminfo.alpha, alpha);
                ea1 = new EulerAngles(k1.cminfo.lyaw, k1.cminfo.lpitch, k1.cminfo.lroll);
                ea2 = new EulerAngles(k2.cminfo.lyaw, k2.cminfo.lpitch, k2.cminfo.lroll);
                ea = EulerInterpol(ea1, ea2, alpha);
                kp.cminfo.lyaw = ea.yaw;
                kp.cminfo.lpitch = ea.pitch;
                kp.cminfo.lroll = ea.roll;

                // pinfo 
                ea1 = new EulerAngles(k1.pinfo.az, k1.pinfo.alt, 0);
                ea2 = new EulerAngles(k2.pinfo.az, k2.pinfo.alt, 0);
                ea = EulerInterpol(ea1, ea2, alpha);
                kp.pinfo.az = ea.yaw;
                kp.pinfo.alt = ea.pitch;
                kp.pinfo.AngularSizeP = lerp(k1.pinfo.AngularSizeP, k2.pinfo.AngularSizeP, alpha);
                // this will produce some inconsistencies, that are corrected by computations on the HMD.
                // depending on the current mode (bool ControlAngularSizeP)
                kp.pinfo.distanceP = lerp(k1.pinfo.distanceP, k2.pinfo.distanceP, alpha);
                kp.pinfo.sizeP = lerp(k1.pinfo.sizeP, k2.pinfo.sizeP, alpha);

                // cinfo
                kp.cinfo.posx = lerp(k1.cinfo.posx, k2.cinfo.posx, alpha);
                kp.cinfo.posy = lerp(k1.cinfo.posy, k2.cinfo.posy, alpha);
                kp.cinfo.posz = lerp(k1.cinfo.posz, k2.cinfo.posz, alpha);
                kp.cinfo.AngularSizeC = lerp(k1.cinfo.AngularSizeC, k2.cinfo.AngularSizeC, alpha);
                // this will produce some inconsistencies, that are corrected by computations on the HMD.
                // depending on the current mode (bool ControlAngularSizeC)
                kp.cinfo.distanceC = lerp(k1.cinfo.distanceC, k2.cinfo.distanceC, alpha);
                kp.cinfo.sizeC = lerp(k1.cinfo.sizeC, k2.cinfo.sizeC, alpha);

                kp.cminfo.time = time;
                kp.set = true;
            }

            return kp;
        }

        public void saveToFile(string filename)
        {
            File.WriteAllText(filename, this.ToJson());
        }

        public void loadFromFile(string filename)
        {
            trajectory t = File.ReadAllText(filename).FromJson<trajectory>();

            allcfg = t.allcfg;
            kp_list = t.kp_list;
        }

    }


}
