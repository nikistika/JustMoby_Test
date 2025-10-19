using _Project.Scripts.Infrastructure.Initialize;
using _Project.Scripts.Installers.Structures;
using _Project.Scripts.Services.Blocks;
using _Project.Scripts.Services.Blocks.Interfaces;
using _Project.Scripts.Services.DragAndDrop;
using _Project.Scripts.Services.Input;
using _Project.Scripts.Services.Saves;
using _Project.Scripts.Services.Tower;
using _Project.Scripts.Services.Tower.Interfaces;
using _Project.Scripts.Services.Zones;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

namespace _Project.Scripts.Installers
{
    public class ServiceInstaller : MonoInstaller
    {
        [SerializeField] private Block blockPrefab;
        [SerializeField] private GameConfig gameConfig;

        public override void InstallBindings()
        {
                BindSignals();
                BindConfigs();
                BindServices();
        }

        private void BindServices()
        {
            Container.Bind<InitializeService>().AsSingle();
            Container.BindInterfacesAndSelfTo<InputService>().AsSingle();
            Container.BindInterfacesAndSelfTo<DragAndDropService>().AsSingle();
            Container.Bind<IElementAnimator>().To<BlockAnimator>().AsSingle();
            Container.Bind<IStackDropValidator>().To<StackDropValidatorDefault>().AsSingle();
            Container.Bind<IStackSaveProvider>().To<StackSaveProviderDefault>().AsSingle();
            Container.Bind<IStackAnimator>().To<StackAnimator>().AsSingle();
            Container.Bind<StackZone>().FromComponentInHierarchy().AsSingle();
            Container.Bind<RemoveZone>().FromComponentInHierarchy().AsSingle();
            Container.Bind<GhostTarget>().AsSingle();
            Container.Bind<EventSystem>().FromComponentInHierarchy().AsSingle();
            Container.Bind<IDataStorage>().To<FileStorage>().AsSingle();
            Container.BindInterfacesAndSelfTo<TowerPersistenceService>().AsSingle();
            Container.BindMemoryPool<Block, BlockPool>().FromComponentInNewPrefab(blockPrefab)
                .UnderTransformGroup("ElementsPool");
        }

        private void BindConfigs()
        {
            Container.BindInstance(gameConfig).AsSingle();
        }

        private void BindSignals()
        {
            SignalBusInstaller.Install(Container);
            Container.DeclareSignal<PointerStarted>().OptionalSubscriber();;
            Container.DeclareSignal<PointerRelease>().OptionalSubscriber();;
            Container.DeclareSignal<StackUpdated>();
            Container.DeclareSignal<MaxStackReached>();
            Container.DeclareSignal<BlockSet>();
            Container.DeclareSignal<BlockMissed>();
            Container.DeclareSignal<BlockRemoved>();
        }
    }
}