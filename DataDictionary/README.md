# DataDictionary

## Overview

**DataDictionary** is a .NET Framework 4.6.2 project for extracting, managing, and documenting Microsoft Dataverse (Dynamics 365) metadata. It provides models and utilities for representing solutions, entities, attributes, and web resources, supporting data dictionary generation and metadata analysis.

## Features

- Models for solutions, entities, attributes, and web resources.
- Utilities for extracting and organizing Dataverse metadata.
- Supports integration with data ingestion and reporting tools.

## Prerequisites

- .NET Framework 4.6.2
- Visual Studio 2017 or later

## Build & Usage

1. Clone the repository and open the solution in Visual Studio.
2. Restore NuGet packages.
3. Build the solution.
4. Reference the project from your data ingestion or reporting application.

## Main Classes

- `DataDictionarySolution`: Represents a Dataverse solution and its components.
- `DataDictionaryEntity`: Represents an entity (table) in Dataverse.
- `DataDictionaryAttribute`: Represents an attribute (column) in Dataverse.
- `DataDictionaryWebResource`: Represents a web resource (e.g., JavaScript) in Dataverse.

## Example
