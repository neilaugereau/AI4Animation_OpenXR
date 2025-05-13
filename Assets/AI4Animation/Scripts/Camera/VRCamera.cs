using System;
using System.Collections;
using UnityEngine;
using AI4Animation;

public class VRCamera : MonoBehaviour {

    public enum MODE {FirstPerson, Embodied, None}
    public enum WAIT {EndOfFrame, FixedUpdate}

    public MODE Mode = MODE.FirstPerson;
    public WAIT Wait = WAIT.EndOfFrame;
    [Range(0f,1f)] public float SmoothTime = 0.1f;

    [Serializable]
    public class FirstPersonSettings {
        public Transform HMD = null;
        public Vector3 DeltaRotation = Vector3.zero;
    }
    public FirstPersonSettings FirstPerson;
    

    [Serializable]
    public class EmbodiedSettings {
        public Transform HMD = null;
        public Transform Head = null;
        public float Distance = 1f;
        public Axis Axis = Axis.ZPositive;
        public Vector3 DeltaRotation = Vector3.zero;
    }
    public EmbodiedSettings Embodied;
    

    private Camera Camera = null;
    private MODE ActiveMode = MODE.None;
    private Vector3 LinearVelocity = Vector3.zero;
    private Vector3 ForwardVelocity = Vector3.zero;
    private Vector3 UpVelocity = Vector3.zero;

    void Awake() {
        Camera = GetComponent<Camera>();
    }

    void Update() {

        if(ActiveMode != Mode) {
            ActiveMode = Mode;
            StopAllCoroutines();
            Debug.Log("Starting Mode: "+ ActiveMode.ToString());
            if(Mode == MODE.FirstPerson) {
                StartCoroutine(FirstPersonCamera());
            }
            if(Mode == MODE.Embodied) {
                StartCoroutine(EmbodiedCamera());
            }
        }
    }

    private Transform GetTarget(Transform target) {
        if(target != null) {
            return target;
        }
        return FindObjectOfType<Actor>().transform;
    }

    private IEnumerator FirstPersonCamera() {
        while(true) {
            switch(Wait) {
                case WAIT.EndOfFrame: yield return new WaitForEndOfFrame(); break;
                case WAIT.FixedUpdate: yield return new WaitForFixedUpdate(); break;
            }
            Transform target = GetTarget(FirstPerson.HMD);
            Transform camera = Camera.transform;
            Matrix4x4 reference = target.GetWorldMatrix();

            //Save previous coordinates
            Vector3 previousPosition = camera.position;
            Quaternion previousRotation = camera.rotation;

            //Calculate new coordinates
            Vector3 position = reference.GetPosition();
            Quaternion rotation = reference.GetRotation() * Quaternion.Euler(FirstPerson.DeltaRotation);

            //Lerp camera from previous to new coordinates
            camera.position = Vector3.SmoothDamp(previousPosition, position, ref LinearVelocity, SmoothTime);
            camera.rotation = Quaternion.LookRotation(
                Vector3.SmoothDamp(previousRotation.GetForward(), rotation.GetForward(), ref ForwardVelocity, SmoothTime).normalized,
                Vector3.SmoothDamp(previousRotation.GetUp(), rotation.GetUp(), ref UpVelocity, SmoothTime).normalized
            );
        }
    }

    private IEnumerator EmbodiedCamera() {
		while(Mode == MODE.Embodied) {
            switch(Wait) {
                case WAIT.EndOfFrame: yield return new WaitForEndOfFrame(); break;
                case WAIT.FixedUpdate: yield return new WaitForFixedUpdate(); break;
            }
            Transform truth = GetTarget(Embodied.HMD);
            Transform target = GetTarget(Embodied.Head);
            Transform camera = Camera.transform;
            Matrix4x4 reference = target.GetWorldMatrix();

            //Save previous coordinates
            Vector3 previousPosition = camera.position;
            Quaternion previousRotation = camera.rotation;

            //Calculate new coordinates
            Vector3 position = reference.GetPosition() - Embodied.Distance * reference.GetAxis(Embodied.Axis);
            position.y = truth.position.y;
            Quaternion rotation = reference.GetRotation() * Quaternion.Euler(Embodied.DeltaRotation);
            //Lerp camera from previous to new coordinates
            camera.position = Vector3.SmoothDamp(previousPosition, position, ref LinearVelocity, SmoothTime);
            camera.rotation = Quaternion.LookRotation(
                Vector3.SmoothDamp(previousRotation.GetForward(), rotation.GetForward(), ref ForwardVelocity, SmoothTime).normalized,
                Vector3.SmoothDamp(previousRotation.GetUp(), rotation.GetUp(), ref UpVelocity, SmoothTime).normalized
            );
		}
    }
}