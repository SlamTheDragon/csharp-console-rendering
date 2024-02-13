  [       ENUM #0                  | STRING #1        | POSITION #2   | FUNCTION #3   | BORDER #4  ]  

AddText
    Add = enum.AddText,              string.text,       int[] { x, y }

AddMagneticText
    Add = enum.AddMagneticText,      string.text,       align

AddDynamicText
    Add = enum.AddDynamicText,       action.textMethod, int[] { x, y }

AddButton
    Add = enum.AddButton,            string.text,       int[] { x, y }, action.onClick, int.border

AddDynamicButton
    Add = enum.AddDynamicButton,     action.textMethod, int[] { x, y }, action.onClick, int.border

AddSlider
    Add = enum.AddSlider,            action.action,     int[] { x, y }, action.onClick, int.border

AddTextInput
    Add = enum.AddTextInput,         action.action,     int[] { x, y }, action.onClick, int.border