using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.NamedValues;
using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Executor.Operations;

public class EntitiesRegistry
{
    private readonly Dictionary<Key, Entity> entities = new();

    public void RegisterMapping(Entity originalEntity, Entity newEntity) =>
        this.entities.Add(CreateKey(originalEntity), newEntity);


    public bool TryGetMapping(Entity originalEntity, [MaybeNullWhen(false)] out Entity newEntity) =>
        this.entities.TryGetValue(CreateKey(originalEntity), out newEntity);

    public bool TryGetMapping(EntityType type, string id, [MaybeNullWhen(false)] out Entity newEntity) =>
        this.entities.TryGetValue(new Key(id, type), out newEntity);


    static Key CreateKey(Entity entity) => new(ChooseId(entity), entity.Type);


    static string ChooseId(Entity entity) =>
        entity.Type switch
        {
            EntityType.NamedValue => ((NamedValueTemplateResource)entity.ArmTemplate).Properties.DisplayName,
            _ => entity.Id
        };
}

record Key(string Id, EntityType Type);