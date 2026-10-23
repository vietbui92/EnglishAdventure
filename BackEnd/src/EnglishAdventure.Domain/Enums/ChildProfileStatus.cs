namespace EnglishAdventure.Domain.Enums;

public enum ChildProfileStatus
{
    /// <summary>Shown to the Parent and can learn.</summary>
    Active = 1,

    /// <summary>Hidden and cannot learn; progress is kept and the profile can be restored.</summary>
    Archived = 2
}
