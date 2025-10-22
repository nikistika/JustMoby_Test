using System;
using System.Collections.Generic;
using _Project.Scripts.Infrastructure.Initialize;
using _Project.Scripts.Installers.Structures;
using _Project.Scripts.Services.Blocks;
using _Project.Scripts.Services.Blocks.Interfaces;
using _Project.Scripts.Services.DragAndDrop.Interfaces;
using _Project.Scripts.Services.Input;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

namespace _Project.Scripts.Services.DragAndDrop
{
    public sealed class DragAndDropService : IInitializeService, IDisposable
    {
        private float _activeHoldTime;
        private float HoldMaxMove => _eventSystem != null ? _eventSystem.pixelDragThreshold : 16f;
        private float HoldMaxMoveSqr => HoldMaxMove * HoldMaxMove;

        private readonly IInputService _inputService;
        private readonly SignalBus _signalBus;
        private readonly EventSystem _eventSystem;
        private readonly GhostTarget _ghost;
        private readonly IElementAnimator _animator;

        private IDragTarget _source;
        private Vector2 _startPos;
        private bool _isHolding;
        private bool _isDisposed;

        private CompositeDisposable _subscriptions;
        private static readonly List<RaycastResult> RaycastBuffer = new(16);

        public DragAndDropService(
            IInputService inputService,
            SignalBus signalBus,
            GhostTarget ghost,
            IElementAnimator animator,
            [Inject(Optional = true)] EventSystem eventSystem)
        {
            _inputService = inputService;
            _signalBus = signalBus;
            _ghost = ghost;
            _animator = animator;
            _eventSystem = eventSystem ?? EventSystem.current;
        }

        public UniTask InitializeAsync()
        {
            _subscriptions = new CompositeDisposable();

            _inputService.CursorDown
                .Subscribe(_ => TryBegin(_inputService.CursorPosition.Value))
                .AddTo(_subscriptions);

            _inputService.CursorMove
                .Subscribe(cursorPos => OnMove(cursorPos))
                .AddTo(_subscriptions);

            Observable.EveryUpdate()
                .Subscribe(_ =>
                {
                    if (_isHolding && !_ghost.IsActive)
                    {
                        StartDragAsync(_inputService.CursorPosition.Value).Forget();
                    }
                })
                .AddTo(_subscriptions);

            _inputService.CursorUp
                .Subscribe(_ => EndAsync(_inputService.CursorPosition.Value).Forget())
                .AddTo(_subscriptions);

            return UniTask.CompletedTask;
        }


        private void TryBegin(Vector2 startPos)
        {
            if (_ghost.IsActive) return;

            _source = RaycastFor<IDragTarget>(startPos);
            if (_source == null) return;

            var scrollRect = RaycastFor<ScrollRect>(startPos);
            var isInScrollRect = scrollRect != null;

            if (!isInScrollRect)
            {
                StartDragAsync(startPos).Forget();
            }

            _startPos = startPos;
        }

        private void OnMove(Vector2 currentPos)
        {
            if (_ghost.IsActive)
            {
                _ghost.MoveTo(currentPos);
                return;
            }

            if (!_isHolding)
            {
                var delta = currentPos - _startPos;
                var movedSqr = delta.sqrMagnitude;

                if (movedSqr >= HoldMaxMoveSqr)
                {
                    var direction = delta.normalized;
                    var angle = Vector2.Angle(Vector2.up, direction);

                    if (angle < 45f)
                    {
                        StartDragAsync(currentPos).Forget();
                    }
                    else
                    {
                        CancelHold();
                    }
                }
            }
        }

        private UniTask StartDragAsync(Vector2 pos)
        {
            _isHolding = false;
            if (_source == null) return UniTask.CompletedTask;

            _ghost.BeginFrom(_source.Rect, pos);
            _source.OnBeginDrag();
            _signalBus.TryFire(new PointerStarted { PointerId = 0 });

            return UniTask.CompletedTask;
        }

        private async UniTask EndAsync(Vector2 pos)
        {
            if (!_ghost.IsActive)
            {
                CancelHold();
                return;
            }

            bool success = false;
            var dropTarget = RaycastFor<IDropTarget>(pos);
            success = dropTarget != null && dropTarget.CanDrop(_source);

            if (success)
            {
                dropTarget.OnDrop(_source);
            }
            else
            {
                if (dropTarget == null)
                {
                    _signalBus.Fire(new BlockMissed());
                    await _animator.PlayMiss(_ghost.RectTransform);
                }
            }

            _ghost.Hide();
            _source?.OnEndDrag(success);
            _source = null;
            _signalBus.TryFire(new PointerRelease { PointerId = 0 });
        }

        private void CancelHold()
        {
            _isHolding = false;
            _source = null;
        }

        private T RaycastFor<T>(Vector2 screenPos) where T : class
        {
            if (_eventSystem == null) return null;

            RaycastBuffer.Clear();
            var pointerData = new PointerEventData(_eventSystem) { position = screenPos };
            _eventSystem.RaycastAll(pointerData, RaycastBuffer);

            for (int index = 0; index < RaycastBuffer.Count; index++)
            {
                var hitGo = RaycastBuffer[index].gameObject;
                var found = FindInterfaceInParents<T>(hitGo);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static T FindInterfaceInParents<T>(GameObject go) where T : class
        {
            var tr = go.transform;
            while (tr != null)
            {
                var behaviours = tr.GetComponents<MonoBehaviour>();
                for (int index = 0; index < behaviours.Length; index++)
                {
                    if (behaviours[index] is T match)
                        return match;
                }

                tr = tr.parent;
            }

            return null;
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _subscriptions?.Dispose();
        }
    }
}