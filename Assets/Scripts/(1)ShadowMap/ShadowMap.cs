using UnityEngine;
using System.Collections;
/// <summary>
/// Accept shadow object
/// </summary>
public class ShadowMap : MonoBehaviour
{

    private Material _mat;
    private Camera _lightCamera;
    void Start()
    {
        MeshRenderer render = GetComponent<MeshRenderer>();
        _mat = render.material;

        foreach (Camera item in Camera.allCameras)
        {
            if (item.CompareTag("LightCamera"))
            {
                _lightCamera = item;
                break;
            }
        }
    }

    void OnWillRenderObject()
    {
        if (_mat != null && _lightCamera != null)
        {
            //Gl
            //_mat.SetMatrix("_ViewProjectionMat", _lightCamera.projectionMatrix * _lightCamera.worldToCameraMatrix);
            // Unity Camera Projectionmatrix Is a Column Matrix of Gl Style: http://docs.Unity3d.com/scriptreference/camera-projectionmatrix.html Projection Z - [- W, W]
            //Real platform-related projection matrix
            _mat.SetMatrix("_ViewProjectionMat", GL.GetGPUProjectionMatrix(_lightCamera.projectionMatrix, true) * _lightCamera.worldToCameraMatrix);
            _mat.SetTexture("_DepthMap", _lightCamera.targetTexture);
            _mat.SetFloat("_NearClip", _lightCamera.nearClipPlane);
            _mat.SetFloat("_FarClip", _lightCamera.farClipPlane);
        }
    }

}
