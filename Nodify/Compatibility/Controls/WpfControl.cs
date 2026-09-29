using System;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Nodify.Compatibility
{
    public class WpfControl : TemplatedControl
    {
        protected PointerEventArgs? currentPointerArgs;

        // 保存 / 恢复而不是写 null：这些 override 可能**嵌套**在本类自己的派发里被调用
        // （最典型：OnMouseDown 里 CaptureMouseSafe() 捕获指针，Avalonia 会立刻同步派发
        // PointerEntered/Exited 之类的事件 → 嵌套一层本类的 override）。
        // 直接写 null 会把外层正在派发中的参数清掉，于是外层后续的 IsMouseCaptured /
        // ReleaseMouseCapture 就报"不在 OnMouseDown/OnMouseUp 里"，整个应用致命错误。
        // 恢复语义对非嵌套调用完全等价（此时 previous 本来就是 null）。

        protected override void OnPointerEntered(PointerEventArgs e)
        {
            var previous = currentPointerArgs;
            currentPointerArgs = e;
            OnMouseEnter(e);
            currentPointerArgs = previous;
            if (!e.Handled)
                base.OnPointerEntered(e);
        }

        protected override  void OnPointerExited(PointerEventArgs e)
        {
            var previous = currentPointerArgs;
            currentPointerArgs = e;
            OnMouseLeave(e);
            currentPointerArgs = previous;
            if (!e.Handled)
                base.OnPointerExited(e);
        }

        protected override  void OnPointerMoved(PointerEventArgs e)
        {
            var previous = currentPointerArgs;
            currentPointerArgs = e;
            OnMouseMove(new MouseMoveEventArgs(e));
            currentPointerArgs = previous;
            if (!e.Handled)
                base.OnPointerMoved(e);
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            var previous = currentPointerArgs;
            currentPointerArgs = e;
            OnMouseDown(new MouseButtonEventArgs(e));
            currentPointerArgs = previous;
            if (!e.Handled)
                base.OnPointerPressed(e);
        }

        protected override  void OnPointerReleased(PointerReleasedEventArgs e)
        {
            var previous = currentPointerArgs;
            currentPointerArgs = e;
            OnMouseUp(new MouseButtonEventArgs(e));
            currentPointerArgs = previous;
            if (!e.Handled)
                base.OnPointerReleased(e);
        }
        
        protected virtual void OnMouseEnter(PointerEventArgs e)
        {
        }

        protected virtual void OnMouseLeave(PointerEventArgs e)
        {
        }

        protected virtual void OnMouseMove(MouseEventArgs e)
        {
        }

        protected virtual void OnMouseDown(MouseButtonEventArgs e)
        {
        }

        protected virtual void OnMouseUp(MouseButtonEventArgs e)
        {
        }

        protected void CaptureMouseSafe()
        {
            if (currentPointerArgs == null)
                throw new InvalidOperationException($"You may only call {nameof(ReleaseMouseCapture)} from within a {nameof(OnMouseUp)} or {nameof(OnMouseDown)} event handler.");
            currentPointerArgs.Pointer.Capture(this);
            this.PropagateMouseCapturedWithin(true);
        }
        
        protected virtual void ReleaseMouseCapture()
        {
            if (currentPointerArgs == null)
                throw new InvalidOperationException($"You may only call {nameof(ReleaseMouseCapture)} from within a {nameof(OnMouseUp)} or {nameof(OnMouseDown)} event handler.");
            currentPointerArgs.Pointer.Capture(null);
            this.PropagateMouseCapturedWithin(false);
        }
        
        /// <summary>
        /// 本元素是否持有指针捕获（WPF <c>UIElement.IsMouseCaptured</c> 的等价物）。
        /// <para>
        /// **属性查询一律不抛**：调用点（如 <c>Connector.OnConnectorDragStarted</c> /
        /// <c>OnMouseUp</c>）在指针派发之外也求值，而 getter 里抛出去的异常会顺着 Avalonia 的
        /// 事件派发一路上抛成**整个应用的致命错误**（表现为"点一下端口就弹崩溃窗"）。
        /// 拿不到当次派发参数时保守返回 <c>false</c>（不认为自己持有捕获）。
        /// </para>
        /// </summary>
        public bool IsMouseCaptured
        {
            get
            {
                if (currentPointerArgs is not { } args)
                    return false;

                return ReferenceEquals(args.Pointer.Captured, this);
            }
        }
    }
}