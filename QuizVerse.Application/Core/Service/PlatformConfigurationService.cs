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
            .FirstOrDefault(p => p.ConfigurationName == SystemConstants.PLATFORM_QUOTE_CONFIGURATION_NAME)
            ?.Values
            ?? SystemConstants.DEFAULT_PLATFORM_QUOTE_JSON;
        var platformQuoteDoc = JsonDocument.Parse(platformQuoteJson);
        string platformQuote = platformQuoteDoc.RootElement.GetProperty(Constants.PLATFORM_QUOTE_KEY).GetString() ?? throw new AppException(Constants.PLATFORM_CONFIGURATION_NULL_ERROR);

        // LOGO
        var platformLogoJson = platformConfigurationList
            .FirstOrDefault(p => p.ConfigurationName == SystemConstants.PLATFORM_LOGO_CONFIGURATION_NAME)
            ?.Values;
        string logoPath;
        if (platformLogoJson == null) logoPath = null;
        else
        {
            var platformLogoDoc = JsonDocument.Parse(platformLogoJson);
            logoPath = platformLogoDoc.RootElement.GetProperty(Constants.PATH_KEY).GetString() ?? throw new AppException(Constants.PLATFORM_CONFIGURATION_NULL_ERROR);
        }

        string logoBase64;
        if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
        {
            byte[] fileBytes = await File.ReadAllBytesAsync(logoPath);
            logoBase64 = Convert.ToBase64String(fileBytes);
        }

        // COLORS
        var platformColor = platformConfigurationList.FirstOrDefault(p => p.ConfigurationName == SystemConstants.PLATFORM_COLOR_CONFIGURATION_NAME);
        string primaryColor = SystemConstants.DEFAULT_PRIMARY_COLOR;
        string secondaryColor = SystemConstants.DEFAULT_SECONDARY_COLOR;
        if (platformColor?.Values != null)
        {
            var platformColorDoc = JsonDocument.Parse(platformColor.Values);
            primaryColor = platformColorDoc.RootElement.GetProperty(Constants.PRIMARY_COLOR_KEY).GetString() ?? SystemConstants.DEFAULT_PRIMARY_COLOR;
            secondaryColor = platformColorDoc.RootElement.GetProperty(Constants.SECONDARY_COLOR_KEY).GetString() ?? SystemConstants.DEFAULT_SECONDARY_COLOR;
        }

        DefaultsColors defaultsColors = new DefaultsColors()
        {
            PrimaryColor = primaryColor,
            SecondaryColor = secondaryColor
        };

        // BUILD RESPONSE
        PlateformConfigurationResponseDTO response = new PlateformConfigurationResponseDTO()
        {
            Quote = platformQuote,
            DefaultsColors = defaultsColors
        };
        if (logoPath != null) response.Logo = logoPath;

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
        string? imagePath;
        if (platformConfigurationRequest.Logo != null)
        {
            imagePath = await commonService.SaveFile(platformConfigurationRequest.Logo, SystemConstants.LOGO_FOLDER_NAME) ?? throw new AppException(Constants.IMAGE_SAVE_ERROR);
            PlatformConfiguration? logoConfig = platformConfigurationList
                .FirstOrDefault(p => p.ConfigurationName == SystemConstants.PLATFORM_LOGO_CONFIGURATION_NAME);
            if (logoConfig == null)
            {
                logoConfig = new PlatformConfiguration
                {
                    ConfigurationName = SystemConstants.PLATFORM_LOGO_CONFIGURATION_NAME,
                    Description = SystemConstants.PLATFORM_LOGO_CONFIGURATION_NAME,
                    Values = JsonSerializer.Serialize(new { Path = imagePath })
                };
                await platformConfigurationRepository.AddAsync(logoConfig);
            }
            else
            {
                logoConfig.Values = JsonSerializer.Serialize(new { Path = imagePath });
                configsToUpdate.Add(logoConfig);
            }
        }
        else
        {
            imagePath = null;
            PlatformConfiguration? logoConfig = platformConfigurationList
                    .FirstOrDefault(p => p.ConfigurationName == SystemConstants.PLATFORM_LOGO_CONFIGURATION_NAME);
            if (logoConfig != null)
                await platformConfigurationRepository.DeleteAsync(logoConfig);
        }

        var files = Directory.GetFiles(SystemConstants.LOGO_PATH);
        foreach (var file in files)
        {
            if (imagePath == null)
            {
                System.IO.File.Delete(file);
                continue;
            }
            if (!file.EndsWith(Path.GetFileName(imagePath), StringComparison.OrdinalIgnoreCase))
            {
                System.IO.File.Delete(file);
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