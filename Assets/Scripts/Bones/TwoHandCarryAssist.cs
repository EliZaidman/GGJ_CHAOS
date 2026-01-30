using UnityEngine;

public class TwoHandCarryAssist : MonoBehaviour
{
    [Header("Hands (AttachRigidbodyToAnother components)")]
    public AttachRigidbodyToAnother leftHand;
    public AttachRigidbodyToAnother rightHand;

    [Header("Assist")]
    public float extraDragWhenTwoHands = 6f;
    public float extraAngularDragWhenTwoHands = 6f;

    Rigidbody _current;
    float _origDrag;
    float _origAngDrag;

    void FixedUpdate()
    {
        if (leftHand == null || rightHand == null) return;

        bool lHold = leftHand.IsHoldingSomething();
        bool rHold = rightHand.IsHoldingSomething();

        if (!lHold || !rHold)
        {
            Clear();
            return;
        }

        var lrb = leftHand.CurrentHeldRigidbody();
        var rrb = rightHand.CurrentHeldRigidbody();

        // only assist if both hands hold the SAME rigidbody
        if (lrb == null || rrb == null || lrb != rrb)
        {
            Clear();
            return;
        }

        if (_current != lrb)
        {
            Clear();
            _current = lrb;
            _origDrag = _current.linearDamping;
            _origAngDrag = _current.angularDamping;
        }

        _current.linearDamping = Mathf.Max(_origDrag, extraDragWhenTwoHands);
        _current.angularDamping = Mathf.Max(_origAngDrag, extraAngularDragWhenTwoHands);
    }

    void Clear()
    {
        if (_current == null) return;
        _current.linearDamping = _origDrag;
        _current.angularDamping = _origAngDrag;
        _current = null;
    }
}
