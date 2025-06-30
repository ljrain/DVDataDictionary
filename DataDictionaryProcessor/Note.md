Class DataIngestor

  Constructor(serviceClient)
    Store the CRM service client

  Method: ProcessSolutions(solutionUniqueNames)
    Start global timer
    Get solution records for each unique name
    For each solution:
      Print solution info
      Get all components in the solution (entities, attributes, web resources, etc.)
    Process all entity components (load entity metadata)
    Print elapsed time

    For the main solution:
      Get logical entities from solutions

    Get all web resource object IDs from solution components
    If any web resources found:
      Print count
      Retrieve web resource records by IDs
      For each web resource:
        For each solution:
          For each web resource in solution:
            If field modifications exist, process them
        Print web resource info
        Decode and parse JavaScript content
        Parse dependencies from XML
        Parse field modifications from JavaScript
        Parse API patterns from JavaScript
        Add parsed web resource to solution's web resource list
        Add parsed dependencies to web resource
        Print summary of parsed results

      Save all JavaScript/web resource data to Dataverse
      Correlate JavaScript field modifications with attribute metadata
      Log schema (entity/attribute metadata)
      Print summary
      Save all attribute metadata to Dataverse
      Stop timer and print elapsed time

    For each solution:
      For each web resource:
        Print web resource and modification info

    For each solution:
      Print grouped entities and attributes

  Private Method: GetSolutions(solutionNames)
    For each solution name:
      Query CRM for solution record
      Add to internal dictionary

  Private Method: GetComponentsInSolution(solution)
    Query CRM for all components in the solution
    For each component:
      Add to solution's component list
      Print component info

  Private Method: ProcessEntities()
    For each solution:
      For each component:
        If component is an entity:
          Query CRM for entity details
          Add entity to solution
          Add logical name to allowed list
          Print entity info

  Private Method: ProcessAttributesAsync()
    For each solution:
      For each component:
        If component is an attribute:
          Query CRM for attribute details
          Add attribute to solution
          Print attribute info

  Private Method: GetWebResourceObjectIds()
    For each solution:
      For each component:
        If component is a web resource:
          Add object ID to list
    Return list of IDs

  Method: GetWebResourcesByObjectIds(objectIds)
    Query CRM for web resources by IDs
    Return list of web resource entities

  Private Method: ParseJavaScript(script, webResource)
    For each known Dataverse JS API pattern:
      If pattern found in script, add to found list
    If webResource provided:
      Parse field modifications and API patterns
      Track modified attributes/tables
    Parse hidden fields for legacy support
    Print found events/actions
    Return found patterns

  Private Method: ParseFieldModifications(script, webResourceId, webResourceName)
    For each line in script:
      For each known field modification pattern:
        If match found, create modification record
        Print found modification
      For advanced patterns:
        If match found, create modification record
        Print found advanced modification
    Return list of modifications

  Private Method: LogSchema()
    Retrieve all entity metadata from CRM
    For each entity in allowed logical names:
      For each attribute:
        Build attribute metadata object
        Handle special cases by attribute type
        Add to solution's attribute metadata list

  Private Method: CorrelateJavaScriptModificationsWithAttributes()
    For each solution:
      Build lookup of attribute metadata by logical name
      For each web resource:
        For each field modification:
          If attribute found:
            Link modification to attribute
            Update attribute metadata based on modification
            Track modifying web resources
    Print summary of correlation

  Private Method: SaveJavascriptToDataverse()
    For each solution:
      For each web resource:
        Upsert web resource record in Dataverse
        Save dependencies for web resource
    Save all JavaScript field modifications

  Private Method: SaveWebResourceDependenciesToDataverse(webResource, webResourceRecordId)
    For each dependency in web resource:
      Upsert dependency record in Dataverse
      Link to attribute metadata if found

  Private Method: SaveJavaScriptFieldModifications()
    For each solution:
      For each web resource:
        For each field modification:
          Upsert modification record in Dataverse

  Private Method: SaveToDataverse()
    For each solution:
      For each attribute metadata:
        Build entity for Dataverse
        Upsert by alternate key (table-logicalname)
        Batch requests for performance

  Private Method: ExecuteBatch(requests)
    Execute batch of create/update requests in Dataverse
    Print results and errors

End Class