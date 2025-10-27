using System;

namespace ValveFileImporter.ValveResourceFormat.KeyValues
{
    /// <summary>
    ///     Represents a <see cref="KeyValues3" /> identifier with a name and GUID.
    /// </summary>
    public readonly struct KV3ID
    {
        public readonly string Name;
        public readonly Guid Id;

        public KV3ID(string name, Guid id)
        {
            Name = name;
            Id = id;
        }

        /// <inheritdoc />
        /// <remarks>
        ///     Returns the <see cref="KV3ID" /> in the format "Name:version{Guid}".
        /// </remarks>
        public override string ToString()
        {
            return $"{Name}:version{{{Id}}}";
        }
    }
}