namespace EnglishAdventure.Domain.Enums;

/// <summary>How a quiz question asks about its word (PD-011). The child always picks one option.</summary>
public enum QuizQuestionForm
{
    /// <summary>The word's picture is shown; the options are English words.</summary>
    PictureToWord = 1,

    /// <summary>The English word is shown; the options are pictures.</summary>
    WordToPicture = 2,

    /// <summary>A sentence with a gap ("I have a ___."); the options are English words.</summary>
    FillInTheBlank = 3
}
