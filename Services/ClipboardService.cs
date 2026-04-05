using HytaleAdmin.Models.Api;

namespace HytaleAdmin.Services;

public class ClipboardService
{
    public EntityDto? CopiedEntity { get; private set; }
    public bool HasEntity => CopiedEntity != null;
    public event Action? OnCopied;

    public void CopyEntity(EntityDto entity)
    {
        CopiedEntity = entity;
        OnCopied?.Invoke();
    }

    public void Clear() => CopiedEntity = null;
}
