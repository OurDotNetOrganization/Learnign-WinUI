using System;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Sys;
using Microsoft.Extensions.Logging;
using SharedLibrary.Components;
using SharedLibrary.Event.Handler;

namespace CoreLibrary.Services;

public class Game : IGame, IDisposable
{
    protected readonly EntitySystem _entitySystem;
    protected readonly ComponentSystem<TransformComponent> _transformComponentSystem;
    protected readonly ILogger<Game> _logger;
    protected readonly IEventHandler _eventHandler;
    public Game(EntitySystem entitySystem, 
                ComponentSystem<TransformComponent> transformComponent, 
                ILogger<Game> logger,
                IEventHandler eventHandler)
    {
        _entitySystem = entitySystem;
        _transformComponentSystem = transformComponent;
        _logger = logger;
        _eventHandler = eventHandler;
        _eventHandler.OnWindowUpdate += OnUpdate;
    }
    public void OnLoad()
    {
        EcsPackedEntityWithWorld entity = _entitySystem.CreateEntity();
        TransformComponent transformComponent = new();
        _transformComponentSystem.TryAddComponent(entity, ref transformComponent);
    }

    public void OnUpdate(object sender, double dt)
    {
        
    }

    public void OnUpdate(double dt)
    {

    }

    public void OnStop()
    {
        throw new System.NotImplementedException();
    }

    public void Dispose()
    {
        _entitySystem?.Dispose();
    }
}