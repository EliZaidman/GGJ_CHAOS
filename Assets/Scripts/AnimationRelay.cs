using UnityEngine;

public class AnimationRelay : MonoBehaviour
{
    public TopDownPhysicsMover3D PhysicsMover3D;

    Animator _animator;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        _animator.SetFloat("Speed", PhysicsMover3D.LinearNormalizedSpeed);
    }
}
