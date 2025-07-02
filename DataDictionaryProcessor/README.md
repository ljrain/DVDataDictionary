# DataDictionaryProcessor

> **Disclaimer:**  
> This software and its documentation are provided as a proof of concept for demonstration and reference purposes only. No warranty, express or implied, is provided. Use at your own risk.

## Overview

**DataDictionaryProcessor** is a console application that generates comprehensive data dictionaries for Microsoft Dataverse (Dynamics 365) solutions. It automates the extraction of metadata about entities, attributes, and web resources, analyzes JavaScript code for field modifications, and correlates this information to create a unified data dictionary.

This tool is designed for Dataverse administrators, developers, and business analysts who need to document and understand the structure and behavior of their Dataverse solutions.

### Key Features

- **Automated Metadata Extraction**: Retrieves comprehensive solution, entity, and attribute metadata from Dataverse
- **JavaScript Analysis**: Parses JavaScript web resources to identify field modifications, visibility changes, and business logic
- **Data Correlation**: Links JavaScript modifications with their corresponding entity attributes and metadata
- **Flexible Output**: Generates structured data dictionary with detailed console reporting
- **Solution-Scoped**: Processes specific solutions rather than entire environments
- **Comprehensive Logging**: Provides detailed progress and timing information

## Prerequisites

### System Requirements

- **Operating System**: Windows (due to .NET Framework 4.6.2 requirement)
- **.NET Framework**: 4.6.2 or higher
- **Development Environment**: Visual Studio 2017 or later (for building from source)

### Dataverse Requirements

- **Dataverse Environment**: Access to a Microsoft Dataverse or Dynamics 365 environment
- **Authentication**: Azure AD application registration with appropriate permissions
- **Permissions**: The application must have sufficient privileges to:
  - Read solution metadata
  - Read entity and attribute metadata
  - Read web resource content
  - Query solution components

### Azure AD Application Setup

1. Register a new application in Azure AD
2. Grant the following API permissions:
   - **Dynamics CRM**: `user_impersonation`
3. Create a client secret
4. Note the following values for configuration:
   - Application (Client) ID
   - Client Secret
   - Tenant ID
   - Dataverse Environment URL

## Installation

### Option 1: Download Release (Recommended)

1. Download the latest release from the GitHub releases page
2. Extract the archive to your desired location
3. Configure the `appsettings.json` file (see Configuration section)
4. Run `DataDictionaryProcessor.exe`

### Option 2: Build from Source

1. **Clone the Repository**:
   ```bash
   git clone https://github.com/ljrain/DVDataDictionary.git
   cd DVDataDictionary/DataDictionaryProcessor
   ```

2. **Restore NuGet Packages**:
   ```bash
   nuget restore DataDictionaryProcessor.sln
   ```

3. **Build the Solution**:
   ```bash
   msbuild DataDictionaryProcessor.sln /p:Configuration=Release
   ```

4. **Configure and Run** (see Configuration and Usage sections below)

## Configuration

### appsettings.json

The application requires configuration in the `appsettings.json` file located in the same directory as the executable:

```json
{
  "CRMURL": "https://your-environment.crm.dynamics.com",
  "CLIENTID": "your-azure-ad-client-id",
  "CLIENTSECRET": "your-azure-ad-client-secret",
  "TENANTID": "your-azure-ad-tenant-id"
}
```

### Configuration Parameters

| Parameter | Description | Example |
|-----------|-------------|---------|
| `CRMURL` | Your Dataverse environment URL | `https://contoso.crm.dynamics.com` |
| `CLIENTID` | Azure AD application client ID | `12345678-1234-1234-1234-123456789012` |
| `CLIENTSECRET` | Azure AD application client secret | `abcdef123456...` |
| `TENANTID` | Azure AD tenant ID | `87654321-4321-4321-4321-210987654321` |

## Usage

### Basic Usage

1. **Configure Connection**: Update `appsettings.json` with your environment details
2. **Run the Application**:
   ```cmd
   DataDictionaryProcessor.exe
   ```
3. **Monitor Progress**: The application will display detailed progress information in the console
4. **Review Results**: Examine the generated data dictionary output

### Current Workflow

The application follows this workflow:

1. **Initialize**: Load configuration and establish Dataverse connection
2. **Collect Metadata**: Retrieve solution, entity, attribute, and web resource metadata
3. **Parse JavaScript**: Analyze JavaScript web resources for field modifications
4. **Correlate Data**: Link JavaScript modifications with attribute metadata
5. **Generate Output**: Create comprehensive data dictionary and display results

### Output Information

The application generates detailed console output including:

- Connection status and timing information
- Solution metadata summary
- Entity and attribute counts
- JavaScript parsing results
- Field modification correlations
- Performance metrics

## Architecture

### High-Level Components

```
Program.cs
    ↓
DictionaryOrchestrator.cs
    ↓
┌─────────────────┬─────────────────┬─────────────────┐
│   DvCollector   │   DvProcessor   │    DvSaver     │
│   (Collection)  │  (Processing)   │   (Storage)    │
└─────────────────┴─────────────────┴─────────────────┘
    ↓                   ↓                   ↓
┌─────────────────┬─────────────────┬─────────────────┐
│ Dataverse API   │ JavaScript      │ Dataverse API   │
│ Metadata        │ Parser          │ Data Storage    │
└─────────────────┴─────────────────┴─────────────────┘
```

### Key Classes

- **Program.cs**: Application entry point and configuration management
- **DictionaryOrchestrator.cs**: Main workflow coordinator
- **DvCollector.cs**: Dataverse metadata collection service
- **DvProcessor.cs**: Data processing and correlation engine
- **DvJavaScriptParser.cs**: JavaScript analysis and field modification detection
- **DvSaver.cs**: Data persistence service (future enhancement)
- **Models/**: Data structure definitions

## Troubleshooting

### Common Issues

#### Connection Problems

**Error**: "Failed to connect to Dynamics CRM"

**Solutions**:
- Verify your `appsettings.json` configuration
- Ensure the Azure AD application has proper permissions
- Check that the client secret is valid and not expired
- Confirm the Dataverse environment URL is correct
- Verify network connectivity to the Dataverse environment

#### Authentication Issues

**Error**: Authentication-related exceptions

**Solutions**:
- Verify the `CLIENTID`, `CLIENTSECRET`, and `TENANTID` values
- Ensure the Azure AD application is granted consent for Dataverse access
- Check that the application registration is in the correct tenant
- Verify that the client secret has not expired

#### Metadata Retrieval Issues

**Error**: Missing entities or attributes in output

**Solutions**:
- Ensure the Azure AD application has sufficient permissions
- Verify that the solutions are published in Dataverse
- Check that the target solutions exist and are accessible
- Review the console output for any filtering or permission warnings

#### JavaScript Parsing Issues

**Error**: JavaScript modifications not detected

**Solutions**:
- Verify that web resources are published and accessible
- Check that JavaScript follows supported patterns (see Developer Guide)
- Review the console output for parsing warnings
- Ensure web resources are in JavaScript format (not TypeScript or other formats)

### Performance Considerations

- **Large Solutions**: Processing time scales with the number of entities and web resources
- **Network Latency**: Connection speed to Dataverse affects metadata retrieval time
- **JavaScript Complexity**: Complex JavaScript files take longer to parse
- **Memory Usage**: Large solutions may require significant memory for metadata storage

### Logging and Diagnostics

The application provides comprehensive console logging including:
- Connection establishment and timing
- Metadata collection progress and counts
- JavaScript parsing results and warnings
- Data correlation statistics
- Overall performance metrics

For additional debugging, monitor the console output for error messages and warnings.

## Security Considerations

### Credential Management

- **Never commit `appsettings.json` to version control** if it contains real credentials
- Use environment-specific configuration files
- Consider using Azure Key Vault or similar for production deployments
- Regularly rotate client secrets
- Follow principle of least privilege for Azure AD application permissions

### Network Security

- Ensure secure communication with Dataverse (HTTPS)
- Consider network restrictions for production environments
- Monitor access logs for unusual activity

### Data Handling

- The application reads metadata but does not modify Dataverse data
- JavaScript content is analyzed but not executed
- Be cautious when processing JavaScript from untrusted sources

## Contributing

### Development Setup

1. Clone the repository
2. Open `DataDictionaryProcessor.sln` in Visual Studio
3. Restore NuGet packages
4. Configure test environment settings
5. Build and run

### Code Style

- Follow existing C# conventions
- Add XML documentation for public methods
- Include appropriate error handling
- Add unit tests for new functionality

### Pull Request Process

1. Create a feature branch
2. Make your changes
3. Add or update tests as needed
4. Update documentation
5. Submit a pull request with a clear description

## License

This solution is provided as-is for internal use in Dataverse environments. See the project license for details.

## Support

For issues, questions, or contributions:

1. Check the [Developer Guide](./developer_guide.md) for detailed technical information
2. Review existing GitHub issues
3. Create a new issue with detailed problem description and steps to reproduce

## Related Documentation

- [Developer Guide](./developer_guide.md) - Comprehensive technical documentation and knowledge transfer guide
- [Project README](../README.md) - Overview of the entire DVDataDictionary solution
- [Architecture Documentation](../docs/) - Detailed architectural analysis and implementation recommendations