using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Script.Utils
{
    public static class ShadowUtils
    {
        private static List<Vector4> _vList = new List<Vector4>();

        /// <summary>
        /// Set the light camera according to the main camera
        /// </summary>
        /// <param name="mainCamera"></param>
        /// <param name="lightCamera"></param>
        public static void SetLightCamera(Camera mainCamera, Camera lightCamera)
        {
            //1, find the 8 vertices of the viewing cone (in the main camera space) n plane (aspect * y, tan(r/2)* n, n) f plane (aspect*y, tan(r/2) * f, f )
            float r = (mainCamera.fieldOfView / 180f) * Mathf.PI;
            //n plane
            Vector4 nLeftUp = new Vector4(-mainCamera.aspect * Mathf.Tan(r / 2) * mainCamera.nearClipPlane, Mathf.Tan(r / 2) * mainCamera.nearClipPlane, mainCamera.nearClipPlane, 1);
            Vector4 nRightUp = new Vector4(mainCamera.aspect * Mathf.Tan(r / 2) * mainCamera.nearClipPlane, Mathf.Tan(r / 2) * mainCamera.nearClipPlane, mainCamera.nearClipPlane, 1);
            Vector4 nLeftDonw = new Vector4(-mainCamera.aspect * Mathf.Tan(r / 2) * mainCamera.nearClipPlane, -Mathf.Tan(r / 2) * mainCamera.nearClipPlane, mainCamera.nearClipPlane, 1);
            Vector4 nRightDonw = new Vector4(mainCamera.aspect * Mathf.Tan(r / 2) * mainCamera.nearClipPlane, -Mathf.Tan(r / 2) * mainCamera.nearClipPlane, mainCamera.nearClipPlane, 1);

            //f plane
            Vector4 fLeftUp = new Vector4(-mainCamera.aspect * Mathf.Tan(r / 2) * mainCamera.farClipPlane, Mathf.Tan(r / 2) * mainCamera.farClipPlane, mainCamera.farClipPlane, 1);
            Vector4 fRightUp = new Vector4(mainCamera.aspect * Mathf.Tan(r / 2) * mainCamera.farClipPlane, Mathf.Tan(r / 2) * mainCamera.farClipPlane, mainCamera.farClipPlane, 1);
            Vector4 fLeftDonw = new Vector4(-mainCamera.aspect * Mathf.Tan(r / 2) * mainCamera.farClipPlane, -Mathf.Tan(r / 2) * mainCamera.farClipPlane, mainCamera.farClipPlane, 1);
            Vector4 fRightDonw = new Vector4(mainCamera.aspect * Mathf.Tan(r / 2) * mainCamera.farClipPlane, -Mathf.Tan(r / 2) * mainCamera.farClipPlane, mainCamera.farClipPlane, 1);

            //2、Transform 8 vertices to world space

            Matrix4x4 mainv2w = mainCamera.transform.localToWorldMatrix;
            //Originally, the matrix here uses mainCamera.cameraToWorldMatrix, but please see: http://docs.unity3d.com/ScriptReference/Camera-cameraToWorldMatrix.html cameraToWorldMatrix returns a GL-style camera space matrix, z is negative, edit with untiy The ones in the device don’t correspond, (it’s also very cheating, can’t it be unified?), so we directly use localToWorldMatrix
            Vector4 wnLeftUp = mainv2w * nLeftUp;
            Vector4 wnRightUp = mainv2w * nRightUp;
            Vector4 wnLeftDonw = mainv2w * nLeftDonw;
            Vector4 wnRightDonw = mainv2w * nRightDonw;
            //
            Vector4 wfLeftUp = mainv2w * fLeftUp;
            Vector4 wfRightUp = mainv2w * fRightUp;
            Vector4 wfLeftDonw = mainv2w * fLeftDonw;
            Vector4 wfRightDonw = mainv2w * fRightDonw;

            //Set the light camera to the center of the mainCamera frustum
            Vector4 nCenter = (wnLeftUp + wnRightUp + wnLeftDonw + wnRightDonw) / 4f;
            Vector4 fCenter = (wfLeftUp + wfRightUp + wfLeftDonw + wfRightDonw) / 4f;

            lightCamera.transform.position = (nCenter + fCenter) / 2f;
            //3、	Find the light view matrix
            Matrix4x4 lgihtw2v = lightCamera.transform.worldToLocalMatrix;
            //Originally lightCamera.worldToCameraMatrix is used here, but for the same reason as above for not using mainCamera.cameraToWorldMatrix, we use worldToLocalMatrix directly
            //4, transform the vertices from world space to light view space
            Vector4 vnLeftUp = lgihtw2v * wnLeftUp;
            Vector4 vnRightUp = lgihtw2v * wnRightUp;
            Vector4 vnLeftDonw = lgihtw2v * wnLeftDonw;
            Vector4 vnRightDonw = lgihtw2v * wnLeftDonw;
            //
            Vector4 vfLeftUp = lgihtw2v * wfLeftUp;
            Vector4 vfRightUp = lgihtw2v * wfRightUp;
            Vector4 vfLeftDonw = lgihtw2v * wfLeftDonw;
            Vector4 vfRightDonw = lgihtw2v * wfRightDonw;

            _vList.Clear();
            _vList.Add(vnLeftUp);
            _vList.Add(vnRightUp);
            _vList.Add(vnLeftDonw);
            _vList.Add(vnRightDonw);

            _vList.Add(vfLeftUp);
            _vList.Add(vfRightUp);
            _vList.Add(vfLeftDonw);
            _vList.Add(vfRightDonw);
            //5、	Find the bounding box (due to the symmetry of the xy axis of the light cone, it is good to find the largest bounding box here, not AABB in the strict sense)
            float maxX = -float.MaxValue;
            float maxY = -float.MaxValue;
            float maxZ = -float.MaxValue;
            float minZ = float.MaxValue;
            for (int i = 0; i < _vList.Count; i++)
            {
                Vector4 v = _vList[i];
                if (Mathf.Abs(v.x) > maxX)
                {
                    maxX = Mathf.Abs(v.x);
                }
                if (Mathf.Abs(v.y) > maxY)
                {
                    maxY = Mathf.Abs(v.y);
                }
                if (v.z > maxZ)
                {
                    maxZ = v.z;
                }
                else if (v.z < minZ)
                {
                    minZ = v.z;
                }
            }
            //5.5 Optimization, if the 8 vertices in the light cone view space z<0, then if n=0, it may happen that the object that should be rendered by the depthmap is clipped by the light cone near clipping surface, so z <0 In the case of moving the light source in the negative direction of the light to avoid this situation            if (minZ < 0)
            {
                lightCamera.transform.position += -lightCamera.transform.forward.normalized * Mathf.Abs(minZ);
                maxZ = maxZ - minZ;
            }

            //6. Determine the projection matrix according to the bounding box. The maximum z of the bounding box is f, Camera.orthographicSize is determined by y max, and Camera.aspect must be set            lightCamera.orthographic = true;
            lightCamera.aspect = maxX / maxY;
            lightCamera.orthographicSize = maxY;
            lightCamera.nearClipPlane = 0.0f;
            lightCamera.farClipPlane = Mathf.Abs(maxZ);
        }
        /// <summary>
        /// Set the light cone according to the scene bounding box
        /// </summary>
        /// <param name="b"></param>
        /// <param name="lightCamera"></param>
        public static void SetLightCamera(Bounds b, Camera lightCamera)
        {
            //1, put the lightCamera in the center of the bounding box

            lightCamera.transform.position = b.center;

            //2, find the light view matrix

            Matrix4x4 lgihtw2v = lightCamera.transform.worldToLocalMatrix;

            //Originally lightCamera.worldToCameraMatrix is used here, but for the same reason as above for not using mainCamera.cameraToWorldMatrix, we use worldToLocalMatrix directly
            //3, transform the vertices from world space to light view space

            Vector4 vnLeftUp = lgihtw2v * new Vector3(b.max.x, b.max.y, b.max.z);
            Vector4 vnRightUp = lgihtw2v * new Vector3(b.max.x, b.min.y, b.max.z);
            Vector4 vnLeftDonw = lgihtw2v * new Vector3(b.max.x, b.max.y, b.min.z);
            Vector4 vnRightDonw = lgihtw2v * new Vector3(b.min.x, b.max.y, b.max.z);
            //
            Vector4 vfLeftUp = lgihtw2v * new Vector3(b.min.x, b.min.y, b.min.z); ;
            Vector4 vfRightUp = lgihtw2v * new Vector3(b.min.x, b.max.y, b.min.z); ;
            Vector4 vfLeftDonw = lgihtw2v * new Vector3(b.min.x, b.min.y, b.max.z); ;
            Vector4 vfRightDonw = lgihtw2v * new Vector3(b.max.x, b.min.y, b.min.z); ;

            _vList.Clear();
            _vList.Add(vnLeftUp);
            _vList.Add(vnRightUp);
            _vList.Add(vnLeftDonw);
            _vList.Add(vnRightDonw);

            _vList.Add(vfLeftUp);
            _vList.Add(vfRightUp);
            _vList.Add(vfLeftDonw);
            _vList.Add(vfRightDonw);

            //4. Find the bounding box (due to the symmetry of the xy axis of the light cone, it is good to find the largest bounding box here, not strictly AABB)

            float maxX = -float.MaxValue;
            float maxY = -float.MaxValue;
            float maxZ = -float.MaxValue;
            float minZ = float.MaxValue;
            for (int i = 0; i < _vList.Count; i++)
            {
                Vector4 v = _vList[i];
                if (Mathf.Abs(v.x) > maxX)
                {
                    maxX = Mathf.Abs(v.x);
                }
                if (Mathf.Abs(v.y) > maxY)
                {
                    maxY = Mathf.Abs(v.y);
                }
                if (v.z > maxZ)
                {
                    maxZ = v.z;
                }
                else if (v.z < minZ)
                {
                    minZ = v.z;
                }
            }

            //4.5 optimization, if the z<0 of the 8 vertices in the light cone view space, then if n=0, the object that should be rendered by the depthmap may be clipped by the light cone near clipping surface, so z <0 In the case of moving the light source in the negative direction of the light to avoid this situation
            if (minZ < 0)
            {
                lightCamera.transform.position += -lightCamera.transform.forward.normalized * Mathf.Abs(minZ);
                maxZ = maxZ - minZ;
            }

            //5. Determine the projection matrix according to the bounding box. The maximum z of the bounding box is f, Camera.orthographicSize is determined by y max, and Camera.aspect must be set
            lightCamera.orthographic = true;
            lightCamera.aspect = maxX / maxY;
            lightCamera.orthographicSize = maxY;
            lightCamera.nearClipPlane = 0.0f;
            lightCamera.farClipPlane = Mathf.Abs(maxZ);
        }

    }
}
