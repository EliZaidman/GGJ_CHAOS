using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class AttachRigidbodyToAnother : MonoBehaviour
{
    Rigidbody _rb;
    FixedJoint _connection;
    public Rigidbody otherRB;
    public InputAction grab;
    public Color Highlight = Color.cyan;
    public AttachRigidbodyToAnother[] otherSystemsInContext;

    public float Force = 1;
    public ForceMode ForceMode = ForceMode.Force;

    private void Awake()
    {
        _rb = GetComponentInParent<Rigidbody>();
        grab.Enable();
    }

    private void FixedUpdate()
    {
        if (otherRB == null)
            return;
        if (_connection != null)
            return;
        if (grab.ReadValue<float>() <= 0.05f)
            return;

        _rb.AddForce((otherRB.position - _rb.position).normalized * Force * grab.ReadValue<float>(), ForceMode.Force);
        otherRB.AddForce((_rb.position -otherRB.position).normalized * Force * grab.ReadValue<float>() * 0.5f, ForceMode.Force);
    }

    // highlight targets
    private void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody == null) return;
        if (other.attachedRigidbody.isKinematic) return;

        if (otherRB == null/* && !otherSystemsInContext.Select(s => s.otherRB).Contains(other.attachedRigidbody)*/)
        {
            otherRB = other.attachedRigidbody;
            otherRB.GetComponent<MeshRenderer>().material.color = Highlight;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        //if (grab.ReadValue<float>() <= 0.05f)
        //{
        //    if (otherRB != null)
        //        otherRB.GetComponent<MeshRenderer>().material.color = Color.gray;

        //    otherRB = null;
        //    return;
        //}

        //if (otherRB == null)
        //{
        //    return;
        //}
        //if (other.attachedRigidbody != otherRB)
        //{
        //    return;
        //}
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.attachedRigidbody != null && other.attachedRigidbody == otherRB)
        {
            otherRB.GetComponent<MeshRenderer>().material.color = Color.gray;
            otherRB = null;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_connection != null)
            return;
        if (otherRB == null)
            return;

        if (collision.rigidbody == otherRB && grab.ReadValue<float>() > 0.05f)
        {
            _connection = otherRB.GetComponent<FixedJoint>();
            if (_connection == null)
                _connection = otherRB.AddComponent<FixedJoint>();

            _connection.connectedBody = _rb;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.rigidbody == otherRB && otherRB != null && grab.ReadValue<float>() <= 0.05f)
        {
            Destroy(_connection);
            otherRB.GetComponent<MeshRenderer>().material.color = Color.gray;
        }
    }
}
