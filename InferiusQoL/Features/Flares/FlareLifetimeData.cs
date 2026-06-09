#nullable disable
using ProtoBuf;
using UnityEngine;

namespace InferiusQoL.Features.Flares;

[ProtoContract]
public sealed class FlareLifetimeData : MonoBehaviour, IProtoEventListener
{
    [ProtoMember(1)]
    public bool hasStarted;

    [ProtoMember(2)]
    public float startedAt;

    [ProtoMember(3)]
    public float baseEnergy;

    [ProtoMember(4)]
    public bool isPaused;

    [ProtoMember(5)]
    public float pausedAt;

    public void OnProtoSerialize(ProtobufSerializer serializer)
    {
        Normalize();
    }

    public void OnProtoDeserialize(ProtobufSerializer serializer)
    {
        Normalize();
    }

    public void Normalize()
    {
        if (!hasStarted)
        {
            startedAt = 0f;
            baseEnergy = 0f;
            isPaused = false;
            pausedAt = 0f;
            return;
        }

        if (baseEnergy < 0f)
        {
            baseEnergy = 0f;
        }

        if (!isPaused)
        {
            pausedAt = 0f;
        }
    }
}
