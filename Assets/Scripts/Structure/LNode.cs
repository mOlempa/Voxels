using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Internal;

public struct LNode
{
    public Vector3Int position;
    //public Vector3 anglesDeg;
    public int thickness;
    public int branchLevel;
    public int prevNodeThickness;
    public ushort branchId;
    public ushort parentBranchId;

    public Quaternion rotation;
    public Quaternion localRotation;

    public void Rotate(Vector3 eulers, [DefaultValue("Space.Self")] Space relativeTo)
    {
        Quaternion quaternion = Quaternion.Euler(eulers.x, eulers.y, eulers.z);
        if (relativeTo == Space.Self)
        {
            localRotation *= quaternion;
        }
        else
        {
            rotation *= Quaternion.Inverse(rotation) * quaternion * rotation;
        }
    }

    public void ApplyLocalRotation()
    {
        rotation = rotation * localRotation;
    }

    public void ResetLocalRotation()
    {
        localRotation = Quaternion.identity;
    }

    public void ApplyLocalRotation(Vector3 localEulers)
    {
        rotation = rotation * Quaternion.Euler(localEulers);
    }

    //
    // Summary:
    //     Applies a rotation of eulerAngles.z degrees around the z-axis, eulerAngles.x
    //     degrees around the x-axis, and eulerAngles.y degrees around the y-axis (in that
    //     order).
    //
    // Parameters:
    //   eulers:
    //     The rotation to apply in euler angles.
    public void Rotate(Vector3 eulers)
    {
        Rotate(eulers, Space.Self);
    }

    public void Rotate(float xAngle, float yAngle, float zAngle, [DefaultValue("Space.Self")] Space relativeTo)
    {
        Rotate(new Vector3(xAngle, yAngle, zAngle), relativeTo);
    }

    public void Rotate(float xAngle, float yAngle, float zAngle)
    {
        Rotate(new Vector3(xAngle, yAngle, zAngle), Space.Self);
    }

    //
    // Summary:
    //     The rotation as Euler angles in degrees.
    public Vector3 eulerAngles
    {
        get
        {
            return rotation.eulerAngles;
        }
        set
        {
            rotation = Quaternion.Euler(value);
        }
    }

    //
    // Summary:
    //     The rotation as Euler angles in degrees relative to the parent transform's rotation.
    public Vector3 localEulerAngles
    {
        get
        {
            return localRotation.eulerAngles;
        }
        set
        {
            localRotation = Quaternion.Euler(value);
        }
    }
}
