﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using AvaloniaEdit.TextMate.Grammars;
using CutCode.CrossPlatform.Helpers;
using CutCode.CrossPlatform.Models;
using CutCode.CrossPlatform.Services;
using CutCode.CrossPlatform.Views;
using Newtonsoft.Json;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace CutCode.CrossPlatform.ViewModels;

public class CodeViewModel : PageBaseViewModel, IRoutableViewModel
{
    public CodeModel Code;

    public CodeViewModel(CodeModel code)
    {
        Initialise(code);
    }

    public CodeViewModel(CodeModel code, IScreen screen)
    {
        HostScreen = screen;
        Initialise(code);
    }

    public void Initialise(CodeModel code)
    {
        Code = code;
        Title = Code.Title;

        var reg = new RegistryOptions(ThemeName.Dark);
        var lang = reg.GetLanguageByExtension(code.Language);
        Language = lang.ToString();
        IsEditEnabled = false;

        var cellsDict = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(code.Cells);
        Cells = new ObservableCollection<CodeCellViewModel?>();
        CellsToViewModel(cellsDict);
        for (int i = 0; i < Cells.Count; i++) Cells[i].IsEditable = false;

        IsCellEmpty = Cells.Count == 0;
        Cells.CollectionChanged += (sender, args) =>
        {
            IsCellEmpty = Cells.Count == 0;
            if (IsCellEmpty && !IsEditEnabled)
            {
                IsEditEnabled = IsCellEmpty;
                for (int i = 0; i < Cells.Count; i++) Cells[i].IsEditable = IsEditEnabled;
            }
        };

        if (ThemeService.Theme == ThemeType.Light) OnLightThemeIsSet();
        else OnDarkThemeIsSet();

        ThemeService.ThemeChanged += (sender, args) =>
        {
            if (ThemeService.Theme == ThemeType.Light) OnLightThemeIsSet();
            else OnDarkThemeIsSet();
        };

        IsFavouritePath = code.IsFavourite ? IconPaths.StarFull : IconPaths.Star;
    }

    public ObservableCollection<CodeCellViewModel?> Cells { get; set; }

    [Reactive] public string Title { get; set; }

    [Reactive] public bool IsCellEmpty { get; set; }

    [Reactive] public bool IsEditEnabled { get; set; }

    [Reactive] public Color BackgroundColor { get; set; }

    [Reactive] public Color BtnColor { get; set; }

    [Reactive] public Color ComboBoxBackground { get; set; }

    [Reactive] public Color ComboBoxBackgroundOnHover { get; set; }

    [Reactive] public Color BarBackground { get; set; }

    [Reactive] public Color TextAreaBackground { get; set; }

    [Reactive] public Color TextAreaForeground { get; set; }

    [Reactive] public Color TextAreaOverlayBackground { get; set; }

    [Reactive] public Color IsFavouriteColor { get; set; }

    [Reactive] public string IsFavouritePath { get; set; }

    [Reactive] public string Language { get; set; }

    private void CellsToViewModel(List<Dictionary<string, string>>? cells)
    {
        Cells.Clear();
        if (cells == null) return;
        foreach (var cell in cells) Cells.Add(new CodeCellViewModel(this, cell["Description"], cell["Code"]));
    }

    private void OnLightThemeIsSet()
    {
        BackgroundColor = Color.Parse("#FCFCFC");
        BarBackground = Color.Parse("#F6F6F6");

        TextAreaBackground = Color.Parse("#ECECEC");
        TextAreaForeground = Color.Parse("#000000");
        TextAreaOverlayBackground = Color.Parse("#E2E2E2");

        ComboBoxBackground = Color.Parse("#ECECEC");
        ComboBoxBackgroundOnHover = Color.Parse("#E2E2E2");

        BtnColor = Color.Parse("#090909");
        IsFavouriteColor = Code.IsFavourite ? Color.Parse("#F7A000") : Color.Parse("#4D4D4D");
    }

    private void OnDarkThemeIsSet()
    {
        BackgroundColor = Color.Parse("#36393F");
        BarBackground = Color.Parse("#303338");

        TextAreaBackground = Color.Parse("#2A2E33");
        TextAreaForeground = Color.Parse("#FFFFFF");
        TextAreaOverlayBackground = Color.Parse("#24272B");

        ComboBoxBackground = Color.Parse("#2A2E33");
        ComboBoxBackgroundOnHover = Color.Parse("#24272B");

        BtnColor = Color.Parse("#F2F2F2");
        IsFavouriteColor = Code.IsFavourite ? Color.Parse("#F7A000") : Color.Parse("#94969A");
    }

    public async void AddCell()
    {
        Cells.Add(new CodeCellViewModel(AddViewModel.Current));
    }

    public async void Cancel()
    {
        GlobalEvents.CancelClicked();
    }

    public async void Save()
    {
        if (Cells.Count > 0 &&
            !Cells.Select(x => x.Description).ToList().Any(string.IsNullOrEmpty) &&
            !Cells.Select(x => x.Document.Text).ToList().Any(string.IsNullOrEmpty))
        {
            var cellsList = Cells.Select(x =>
                new Dictionary<string, string>
                {
                    { "Description", x.Description },
                    { "Code", x.Document.Text }
                }).ToList();

            CodeModel editedCode = new CodeModel(Title, cellsList, Language,
                new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(), Code.IsFavourite);
            editedCode.SetId(Code.Id);

            if (DataBase.EditCode(editedCode))
            {
                IsEditEnabled = false;
                for (int i = 0; i < Cells.Count; i++) Cells[i].IsEditable = false;
            }
            else
            {
                NotificationService.CreateNotification("Error", "Error, Unable to save the changes", 5);
            }
        }
        else
        {
            NotificationService.CreateNotification("Warning", "Please Fill the Empty fields", 2);
        }
    }

    public async void EditCommand()
    {
        IsEditEnabled = true;
        for (int i = 0; i < Cells.Count; i++) Cells[i].IsEditable = true;
    }

    public async void FavouriteCommand()
    {
        bool favUpdate = DatabaseService.Current.FavModify(Code);
        if (favUpdate)
        {
            Code.IsFavourite = !Code.IsFavourite;
            IsFavouritePath = Code.IsFavourite ? IconPaths.StarFull : IconPaths.Star;

            if (ThemeService.Current.Theme == ThemeType.Light)
                IsFavouriteColor = Code.IsFavourite ? Color.Parse("#F7A000") : Color.Parse("#4D4D4D");
            else IsFavouriteColor = Code.IsFavourite ? Color.Parse("#F7A000") : Color.Parse("#94969A");
        }
        else
        {
            NotificationService.CreateNotification("Error", "Error, Unable to save the changes!", 3);
        }
    }

    public async void DeleteCode()
    {
        bool delete = DatabaseService.Current.DelCode(Code);
        if (delete)
        {
            PageService.ExternalPage = new HomeView();
        }
        // if it wasn't deleted, we will show notificaiton
    }

    public async void Share()
    {
        // will be implemented later
    }

    public static void DeleteCell(CodeViewModel vm, CodeCellViewModel cell)
    {
        if (vm.IsEditEnabled) vm.Cells.Remove(cell);
    }

    public string? UrlPathSegment => Guid.NewGuid().ToString().Substring(0, 5);
    public IScreen HostScreen { get; }
}