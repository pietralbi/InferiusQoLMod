#nullable disable
using ProtoBuf;
using UnityEngine;

namespace InferiusQoL.Features.InventoryStacking;

[ProtoContract]
public sealed class MRStackData : MonoBehaviour, IProtoEventListener
{
	[ProtoMember(1)]
	public int amount = 1;

	public void OnProtoSerialize(ProtobufSerializer serializer)
	{
		amount = Mathf.Max(1, amount);
	}

	public void OnProtoDeserialize(ProtobufSerializer serializer)
	{
		amount = Mathf.Max(1, amount);
	}
}
