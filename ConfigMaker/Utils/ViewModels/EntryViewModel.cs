﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfigMaker.Utils.ViewModels
{
    public class EntryViewModel: BindableBase
    {
        bool _isEnabled = true;
        bool _isChecked = false;
        string _content = string.Empty;
        string _key = null;
        bool _isFocused = false;

        public event EventHandler Click;

        public EntryViewModel()
        {
            this.PropertyChanged += (_, arg) =>
            {
                if (this._isEnabled && arg.PropertyName == nameof(EntryViewModel.IsChecked))
                    this.Click.Invoke(this, null);
            };
        }

        public string Key
        {
            get => _key;
            set => this.SetProperty(ref _key, value);
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => this.SetProperty(ref _isEnabled, value);
        }

        public bool IsChecked
        {
            get => _isChecked;
            set => this.SetProperty(ref _isChecked, value);
        }

        public string Content
        {
            get => _content;
            set => this.SetProperty(ref _content, value);
        }

        public bool IsFocused
        {
            get => this._isFocused;
            set => this.SetProperty(ref _isFocused, value);
        }
    }
}