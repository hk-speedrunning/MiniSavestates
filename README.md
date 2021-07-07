### How to use
Download the `Assembly-CSharp.dll` file and replace it in your hollow knight install's `Managed` folder.  
The keybinds are customizable in `<saves location>/minisavestates.json` and by default it is
```json
{
    "LoadStateButton": "f1",
    "SaveStateButton": "f2"
}
```
When you save a state it gets saved to the file `<saves location>/minisavestates-saved.json`

### Building

1. Copy vanilla managed folder into a new folder named `vanilla` in this repository.
2. run `dotnet build`.
3. The output will be in `out`.