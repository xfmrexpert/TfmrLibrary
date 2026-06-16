namespace TfmrLib.FEM
{
    /// <summary>
    /// Implemented by FEM entities that carry a stable, unique <see cref="Name"/>.
    /// The name doubles as the lookup key in <see cref="NamedCollection{T}"/>, so it is
    /// expected to be immutable for the lifetime of the item (declare it <c>get; init;</c>).
    /// </summary>
    public interface INamed
    {
        string Name { get; }
    }
}
