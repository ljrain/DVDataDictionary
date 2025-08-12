# DVDataDictionary Glossary

This glossary provides definitions for technical terms, concepts, and acronyms used throughout the DVDataDictionary documentation and user interface.

## A

**API (Application Programming Interface)**  
A set of protocols and tools for building software applications. In Dataverse context, refers to the web services used to interact with data and metadata.

**Attribute**  
The technical term for a field in Microsoft Dataverse. Contains data such as text, numbers, dates, or choices. Synonymous with "field" in business terminology.

**Authentication**  
The process of verifying the identity of a user or application. DVDataDictionary uses Azure Active Directory authentication to securely connect to Dataverse.

**Azure Active Directory (Azure AD)**  
Microsoft's cloud-based identity and access management service. Used for authenticating and authorizing access to Dataverse environments.

## B

**Business Logic**  
Custom rules and processes implemented through configuration or code that define how an application behaves. In Dataverse, this includes business rules, workflows, and JavaScript customizations.

**Business Rule**  
A declarative way to implement business logic in Dataverse without writing code. Can control field visibility, requirements, and values based on conditions.

## C

**Client ID**  
A unique identifier assigned to an Azure AD application registration. Used for authentication when connecting to Dataverse.

**Client Secret**  
A password-like credential for an Azure AD application. Used along with the Client ID for secure authentication.

**Component**  
Individual elements that make up a Dataverse solution, including entities, fields, forms, views, web resources, and business rules.

**Console Application**  
A command-line program that runs in a text-based interface. DVDataDictionary's primary interface for generating data dictionaries.

**Customization**  
Modifications made to a Dataverse environment beyond the default configuration, including custom entities, fields, forms, and business logic.

## D

**Data Dictionary**  
Comprehensive documentation that describes the structure, contents, and context of data in a system. Includes field definitions, relationships, and business rules.

**Data Type**  
The category of data that a field can contain, such as text, whole number, currency, date/time, or choice (picklist).

**Dataverse**  
Microsoft's cloud-based data platform that provides secure storage and rich data types for business applications. Formerly known as Common Data Service (CDS).

**DataDictionaryProcessor**  
The console application component of DVDataDictionary that handles automated metadata extraction and documentation generation.

**Dual-Environment Configuration**  
A setup where DVDataDictionary scans one Dataverse environment (source) and stores the generated documentation in another environment (storage). This enables separation of operational and documentation environments.

**Default Value**  
A predetermined value automatically assigned to a field when a new record is created, often set through JavaScript or business rules.

**Dependency**  
A relationship where one component relies on another component to function correctly. Important for understanding change impact.

## E

**Entity**  
The technical term for a table in Microsoft Dataverse. Contains records (rows) with attributes (fields). Examples include Account, Contact, or custom entities.

**Entity Relationship**  
Connections between entities that define how data relates across tables. Common types include one-to-many, many-to-one, and many-to-many relationships.

**Environment**  
An isolated Dataverse instance containing solutions, entities, and data. Organizations typically have development, test, and production environments.

## F

**Field**  
The business term for an attribute in Dataverse. A single data element within an entity record, such as "First Name" or "Annual Revenue."

**Field Modification**  
Changes to field behavior implemented through JavaScript, such as making fields visible/hidden, required/optional, or setting default values.

**Form**  
The user interface for viewing and editing entity records in Dataverse applications. Can contain fields, sections, tabs, and embedded components.

**formContext**  
The modern JavaScript API object for interacting with Dataverse forms and fields. Replaces the legacy Xrm.Page object.

## I

**Integration**  
The process of connecting Dataverse with other systems or applications to share data and functionality.

## J

**JavaScript**  
A programming language used to implement custom business logic on Dataverse forms. Analyzed by DVDataDictionary to understand field modifications.

**JavaScript Analysis**  
The process of examining JavaScript code to identify how it modifies field behavior, visibility, requirements, and values.

**JSON (JavaScript Object Notation)**  
A lightweight data format used for configuration files, including DVDataDictionary's appsettings.json file.

## M

**Metadata**  
Data about data - information that describes the structure, properties, and relationships of entities and attributes in Dataverse.

**Metadata Analysis**  
The process of extracting and documenting the structure and configuration of Dataverse solutions, including entities, attributes, relationships, and web resources.

**Multi-Environment Scanning**  
The capability to scan metadata from one Dataverse environment while storing the generated documentation in a different environment.

## L

**Logical Name**  
The unique identifier for entities and fields in Dataverse, used for programmatic access. For example, "account" is the logical name for the Account entity.

## M

**Metadata**  
Data that describes other data. In Dataverse, includes information about entities, fields, relationships, and configurations rather than actual business records.

**Model Driven App**  
A type of application built on Dataverse that provides a responsive user interface based on entity metadata and relationships.

**Modification**  
A change to standard Dataverse behavior, typically implemented through JavaScript, business rules, or configuration changes.

## O

**Object Type Code**  
A numeric identifier assigned to each entity in Dataverse, used internally for entity identification.

## P

**Pattern Recognition**  
The process of identifying common JavaScript coding patterns that indicate specific types of field modifications or business logic.

**Plugin**  
Custom .NET code that runs within the Dataverse platform to extend functionality. DVDataDictionary includes a plugin version for in-environment execution.

## R

**Relationship**  
A connection between entities that defines how records relate to each other. Enables linking related information across different entities.

**Required Level**  
A field property that determines whether users must provide a value. Can be set to Required, Recommended, or Optional.

## S

**SDK (Software Development Kit)**  
A collection of tools, libraries, and documentation for developing applications that interact with Dataverse.

**Solution**  
A container for customizations in Dataverse. Enables packaging, distributing, and managing related components as a unit.

**Solution Component**  
Individual elements contained within a solution, such as entities, fields, forms, views, and web resources.

## T

**Table**  
The modern term for an entity in Dataverse. Contains rows (records) with columns (fields/attributes).

**Tenant**  
An Azure AD organization instance that represents a single organization and contains users, groups, and applications.

**Tenant ID**  
A unique identifier for an Azure AD tenant, used for authentication and authorization.

## S

**Schema Name**  
The technical name of an entity or attribute used in code and API calls. Often different from the display name shown to users.

**Solution**  
A container for customizations in Dataverse that groups related components together for deployment and management.

**Solution Component**  
Individual pieces that make up a Dataverse solution, including entities, attributes, web resources, and other customizations.

**Source Environment**  
In dual-environment configuration, the Dataverse environment that is scanned for metadata and solution information. Configured in the DATAVERSE section of appsettings.json.

**Storage Environment**  
In dual-environment configuration, the Dataverse environment where the generated data dictionary documentation is stored. Configured in the DATADICTIONARY section of appsettings.json.

## T

**Tenant ID**  
A unique identifier for an Azure Active Directory tenant, required for authentication to Dataverse environments.

## U

**Unified Interface**  
The modern user experience framework for Dataverse applications, providing consistent and responsive interfaces across devices.

**User Context**  
The security context under which DVDataDictionary operates, determining what metadata and data can be accessed.

## V

**Visibility**  
A field property that determines whether users can see a field on forms. Can be controlled through business rules or JavaScript.

## W

**Web Resource**  
Files uploaded to Dataverse that can be used in applications, including JavaScript files, CSS stylesheets, images, and HTML pages.

**Web Resource Analysis**  
The process of examining web resources, particularly JavaScript files, to understand their impact on form behavior and field modifications.

**Workflow**  
An automated business process in Dataverse that can perform actions based on specified conditions and triggers.

## X

**Xrm.Page**  
The legacy JavaScript API object for interacting with Dataverse forms. Being replaced by the modern formContext API but still supported for backward compatibility.

---

## Common Acronyms

**AD** - Active Directory  
**API** - Application Programming Interface  
**CDS** - Common Data Service (former name for Dataverse)  
**CRM** - Customer Relationship Management  
**CSV** - Comma-Separated Values  
**GUID** - Globally Unique Identifier  
**HTML** - HyperText Markup Language  
**HTTP/HTTPS** - HyperText Transfer Protocol (Secure)  
**JSON** - JavaScript Object Notation  
**REST** - Representational State Transfer  
**SDK** - Software Development Kit  
**SQL** - Structured Query Language  
**UI** - User Interface  
**URL** - Uniform Resource Locator  
**XML** - eXtensible Markup Language  

---

## Usage Notes

- **Field vs. Attribute**: In business contexts, use "field." In technical contexts, "attribute" is more precise.
- **Entity vs. Table**: Modern documentation prefers "table," but "entity" remains common in technical contexts.
- **Dataverse vs. CDS**: Dataverse is the current name; CDS (Common Data Service) is the legacy name.
- **Xrm.Page vs. formContext**: Use formContext for new development; Xrm.Page is supported but deprecated.

---

*This glossary is maintained as part of the DVDataDictionary project documentation. For questions about specific terms or to suggest additions, please refer to the project's GitHub repository.*