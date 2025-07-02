# DVDataDictionary - Automated Data Dictionary Generation for Microsoft Dataverse

## Overview

**DVDataDictionary** is a comprehensive solution for automatically generating detailed data dictionaries from Microsoft Dataverse (Dynamics 365) environments. The solution analyzes solution metadata, entity structures, field configurations, and JavaScript customizations to produce thorough documentation that helps organizations understand and maintain their Dataverse implementations.

![image](https://github.com/user-attachments/assets/113ace51-913f-4139-9e49-ad202681c08a)

## Why DVDataDictionary?

Managing Dataverse customizations becomes increasingly complex as organizations scale their implementations. DVDataDictionary addresses critical documentation challenges:

- **Automated Documentation**: Eliminates manual effort in creating and maintaining data dictionaries
- **JavaScript Analysis**: Discovers hidden business logic by analyzing form scripts and field modifications
- **Comprehensive Coverage**: Documents entities, attributes, relationships, and customizations in one unified view
- **Metadata Correlation**: Links JavaScript behaviors with field metadata for complete understanding
- **Centralized Storage**: Saves all documentation data directly to Dataverse for unified access and reporting
- **Change Tracking**: Helps identify what customizations exist and how they interact

## Project Structure

The DVDataDictionary solution consists of two main projects:

### DataDictionary
Core library containing:
- **Plugin Architecture**: Dataverse plugin for in-environment execution
- **Data Models**: Comprehensive models for solutions, entities, attributes, and web resources
- **Metadata Processing**: Core logic for extracting and organizing Dataverse metadata
- *This project will be a duplicate of **DataDictionaryProcessor** upon testing and verification to run under Dataverse.*

### DataDictionaryProcessor  
Console application providing:
- **Standalone Processing**: Command-line tool for automated data dictionary generation
- **JavaScript Analysis**: Advanced parsing of JavaScript code for field modifications
- **Batch Processing**: Efficient handling of large Dataverse solutions
- **Centralized Output**: Automatically saves results to Dataverse for access within a Model Driven App

## Key Features

### Metadata Extraction
- Thorough solution component analysis (entities, attributes and relationships)
- Entity relationship mapping
- Field-level metadata including data types, constraints, and configurations
- Web resource inventory and analysis

### JavaScript Analysis
- **Field Modification Detection**: Identifies JavaScript that modifies field visibility, requirements, or values
- **Business Logic Discovery**: Documents custom behaviors implemented in form scripts
- **Dependency Tracking**: Maps relationships between scripts and the fields they affect
- **API Pattern Recognition**: Identifies common Dataverse API usage patterns

### Documentation Generation
- **Unified Data Dictionaries**: Single source of truth for solution documentation
- **Cross-Reference Correlation**: Links metadata with behavioral modifications
- **Centralized Storage**: All documentation is saved to Dataverse for access and reporting within a Model Driven App
- **Comprehensive Coverage**: Documents both standard configuration and custom behaviors

## Getting Started

### Prerequisites
- **.NET Framework 4.6.2** or higher
- **Microsoft Dataverse Environment** with appropriate permissions
- **Azure AD Application Registration** for authentication

### Quick Start

1. **Clone the Repository**  
   `git clone https://github.com/ljrain/DVDataDictionary.git`  
   `cd DVDataDictionary`

2. **Choose Your Approach**

   * Console Application (Recommended)**  
   - Navigate to [DataDictionaryProcessor/README.md](./DataDictionaryProcessor/README.md)  
   - Follow setup instructions for standalone processing


3. **Configure Authentication**  
   - Set up Azure AD application with Dataverse permissions  
   - Configure connection settings (see component-specific documentation)

4. **Run Your First Analysis**  
   - Specify target Dataverse solutions  
   - Execute data dictionary generation  
   - Review generated documentation within your Model Driven App

## Documentation

- **[DataDictionaryProcessor Guide](./DataDictionaryProcessor/README.md)** - Console application setup and usage
- **[DataDictionary Plugin Guide](./DataDictionary/README.md)** - Plugin deployment and configuration
- **[Developer Guide](./DataDictionaryProcessor/developer_guide.md)** - Comprehensive technical documentation and knowledge transfer guide
- **[Architecture Documentation](./docs/)** - Detailed architectural analysis and recommendations

## Sample Output

The solution generates comprehensive data dictionaries stored in Dataverse, accessible and reportable within your Model Driven App.

## Use Cases

### Solution Documentation
Generate comprehensive documentation for Dataverse solutions during development, deployment, or maintenance phases.

### Custom Code Analysis  
Understand the impact of JavaScript customizations on form behavior and field interactions.

### Change Impact Assessment
Analyze how modifications to entities or fields might affect existing customizations.

### Knowledge Transfer
Provide new team members with complete understanding of solution structure and behaviors.

### Compliance and Auditing
Document system configurations and customizations for regulatory compliance.

## Technology Stack

- **.NET Framework 4.6.2**: Core runtime environment
- **Microsoft Dataverse SDK**: Native integration with Dataverse APIs
- **JavaScript Parsing**: Custom analysis engine for form script processing
- **Dataverse Storage**: All documentation is saved centrally for unified access

## Contributing

We welcome contributions! Please see our contributing guidelines:

1. Review the [Developer Guide](./DataDictionaryProcessor/developer_guide.md) for technical details
2. Check existing issues and feature requests
3. Create feature branches for new functionality
4. Include appropriate tests and documentation
5. Submit pull requests with clear descriptions

## Support

For questions, issues, or feature requests:

1. **Technical Documentation**: Check the [Developer Guide](./DataDictionaryProcessor/developer_guide.md)
2. **Architecture Questions**: Review the [Architecture Documentation](./docs/)
3. **Issues**: Create a GitHub issue with detailed problem description
4. **Feature Requests**: Submit enhancement requests through GitHub issues

## License

This solution is provided as-is for use with Microsoft Dataverse environments. See individual component licenses for specific terms.

---

*DVDataDictionary helps organizations maintain comprehensive, up-to-date documentation of their Dataverse implementations, reducing technical debt and improving development efficiency.*
