namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class PlateformConfigurationResponseDTO
{
    public string Quote { get; set; } = null!;
    public DefaultsColors DefaultsColors { get; set; } = null!;
    public string Logo { get; set; } = null!;
}

public class DefaultsColors
{
    public string PrimaryColor { get; set; } = null!;
    public string SecondaryColor { get; set; } = null!;
}