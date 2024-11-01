using System;

namespace Backrooms.Serialization;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class DontSerializeAttribute() : Attribute()
{
}