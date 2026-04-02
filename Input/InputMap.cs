using Stride.Input;

namespace HytaleAdmin.Input;

public static class InputMap
{
    public static bool IsPressed(InputManager input, InputAction action) => action switch
    {
        InputAction.LoadMap => input.IsKeyPressed(Keys.Enter),
        InputAction.Cancel => input.IsKeyPressed(Keys.Escape),
        InputAction.Quit => input.IsKeyPressed(Keys.Q),
        _ => false
    };

    public static bool IsDown(InputManager input, InputAction action) => action switch
    {
        _ => false
    };

    public static bool IsShiftDown(InputManager input) =>
        input.IsKeyDown(Keys.LeftShift) || input.IsKeyDown(Keys.RightShift);

    public static bool IsMouseButtonDown(InputManager input, MouseButton button) =>
        input.IsMouseButtonDown(button);

    public static bool IsMouseButtonPressed(InputManager input, MouseButton button) =>
        input.IsMouseButtonPressed(button);

    public static bool IsMouseButtonReleased(InputManager input, MouseButton button) =>
        input.IsMouseButtonReleased(button);
}
