using System.Text.Json;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service;

public class PlatformConfigurationService(IGenericRepository<PlatformConfiguration> platformConfigurationRepository, ICommonService commonService) : IPlatformConfigurationService
{

    public async Task<PlateformConfigurationResponseDTO> GetPlatformConfigurations()
    {
        List<PlatformConfiguration> platformConfigurationList = await platformConfigurationRepository.GetAllAsync();

        // QUOTE
        var platformQuoteJson = platformConfigurationList
            .First(p => p.ConfigurationName == SystemConstants.PLATFORM_QUOTE_CONFIGURATION_NAME)
            .Values;
        var platformQuoteDoc = JsonDocument.Parse(platformQuoteJson);
        string platformQuote = platformQuoteDoc.RootElement.GetProperty(Constants.PLATFORM_QUOTE_KEY).GetString() ?? throw new AppException(Constants.PLATFORM_CONFIGURATION_NULL_ERROR);

        // LOGO 
        var platformLogoJson = platformConfigurationList
            .First(p => p.ConfigurationName == SystemConstants.PLATFORM_LOGO_CONFIGURATION_NAME)
            .Values;
        var platformLogoDoc = JsonDocument.Parse(platformLogoJson);
        string logoPath = platformLogoDoc.RootElement.GetProperty(Constants.PATH_KEY).GetString() ?? throw new AppException(Constants.PLATFORM_CONFIGURATION_NULL_ERROR);

        string logoBase64;
        if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
        {
            byte[] fileBytes = await File.ReadAllBytesAsync(logoPath);
            logoBase64 = Convert.ToBase64String(fileBytes);
        }

        // COLORS
        var platformColorJson = platformConfigurationList
            .First(p => p.ConfigurationName == SystemConstants.PLATFORM_COLOR_CONFIGURATION_NAME)
            .Values;
        var platformColorDoc = JsonDocument.Parse(platformColorJson);
        string primaryColor = platformColorDoc.RootElement.GetProperty(Constants.PRIMARY_COLOR_KEY).GetString() ?? throw new AppException(Constants.PLATFORM_CONFIGURATION_NULL_ERROR);
        string secondaryColor = platformColorDoc.RootElement.GetProperty(Constants.SECONDARY_COLOR_KEY).GetString() ?? throw new AppException(Constants.PLATFORM_CONFIGURATION_NULL_ERROR);

        DefaultsColors defaultsColors = new DefaultsColors()
        {
            PrimaryColor = primaryColor,
            SecondaryColor = secondaryColor
        };

        // BUILD RESPONSE
        PlateformConfigurationResponseDTO response = new PlateformConfigurationResponseDTO()
        {
            Quote = platformQuote,
            Logo = logoPath,
            DefaultsColors = defaultsColors
        };

        return response;
    }

    public async Task<string> UpdatePlatformConfigurations(PlatformConfigurationRequestDTO platformConfigurationRequest)
    {
        List<PlatformConfiguration> platformConfigurationList = await platformConfigurationRepository.GetAllAsync();
        List<PlatformConfiguration> configsToUpdate = new();

        // QUOTE
        var quoteConfig = platformConfigurationList
            .First(p => p.ConfigurationName == SystemConstants.PLATFORM_QUOTE_CONFIGURATION_NAME);
        quoteConfig.Values = JsonSerializer.Serialize(new { PlatformQuote = platformConfigurationRequest.Quote });
        configsToUpdate.Add(quoteConfig);

        // LOGO
        if (platformConfigurationRequest.Logo != null)
        {
            string? imagePath;
            imagePath = await commonService.SaveFile(platformConfigurationRequest.Logo, SystemConstants.LOGO_FOLDER_NAME);
            var logoConfig = platformConfigurationList
                .First(p => p.ConfigurationName == SystemConstants.PLATFORM_LOGO_CONFIGURATION_NAME);
            logoConfig.Values = JsonSerializer.Serialize(new { Path = imagePath });
            configsToUpdate.Add(logoConfig);

            var files = Directory.GetFiles(SystemConstants.LOGO_PATH);
            foreach (var file in files)
            {
                if (!file.EndsWith(Path.GetFileName(imagePath)!, StringComparison.OrdinalIgnoreCase))
                {
                    System.IO.File.Delete(file);
                }
            }
        }

        // COLORS
        var colorConfig = platformConfigurationList
            .First(p => p.ConfigurationName == SystemConstants.PLATFORM_COLOR_CONFIGURATION_NAME);
        colorConfig.Values = JsonSerializer.Serialize(new
        {
            PrimaryColor = platformConfigurationRequest.DefaultsColors.PrimaryColor,
            SecondaryColor = platformConfigurationRequest.DefaultsColors.SecondaryColor
        });
        configsToUpdate.Add(colorConfig);

        await platformConfigurationRepository.UpdateRangeAsync(configsToUpdate);

        return Constants.PLATFORM_CONFIGURATION_UPDATE_SUCCESS;
    }
}
