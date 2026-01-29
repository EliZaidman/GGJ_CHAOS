using UnityEngine;

public class testPhysicsMove : MonoBehaviour
{
    Animator _animator;
    public Transform[] bonesToReset;
    (Vector3, Quaternion)[] resets;

    private void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        resets = new (Vector3, Quaternion)[bonesToReset.Length];
    }

    private void LateUpdate()
    {
        for (int i = 0; i < bonesToReset.Length; i++)
        {
            Transform bone = bonesToReset[i];
            resets[i] = (bone.localPosition, bone.localRotation);
        }

        _animator.Update(Time.deltaTime);

        for (int i = 0; i < bonesToReset.Length; i++)
        {
            Transform bone = bonesToReset[i];
            bone.localPosition = resets[i].Item1;
            bone.localRotation = resets[i].Item2;
        }
    }
}
