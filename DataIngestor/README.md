# DataIngestor

## Overview

**DataIngestor** is a .NET Framework 4.6.2 project for ingesting Microsoft Dataverse (Dynamics 365) metadata, solution components, and web resources. It retrieves solution and entity metadata, decodes and analyzes JavaScript web resources, and saves structured metadata to Dataverse custom tables for reporting or documentation.

## Features

- Retrieve solutions and their components from Dataverse.
- Extract entity and attribute metadata.
- Download and decode JavaScript web resources.
- Analyze JavaScript for Dataverse API usage and hidden fields.
- Save metadata and web resource content to custom Dataverse tables.

## Prerequisites

- .NET Framework 4.6.2
- Visual Studio 2017 or later
- Access to a Microsoft Dataverse (Dynamics 365) environment
- Valid credentials and permissions for Dataverse API

## Build & Usage

1. Clone the repository and open the solution in Visual Studio.
2. Restore NuGet packages.
3. Update connection settings as needed.
4. Build the solution.
5. Run the application, providing the required solution unique names.

## Main Classes

- `InjestorV2`: Main orchestrator for retrieving, processing, and saving metadata and web resources.
- `DataDictionarySolution`, `DataDictionaryWebResource`: Models for solution and web resource data.

## Example
