using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using SharpGen.Logging;

namespace SharpGen.Interactive
{
    public class SharpGenModel : INotifyPropertyChanged
    {
        public class ProgressReporter : IProgressReport
        {
            private readonly SharpGenModel model;

            public ProgressReporter(SharpGenModel model)
            {
                this.model = model;
            }

            public void FatalExit(string message)
            {
                model.ProgressMessage = "SharpGen failed";
            }

            public bool ProgressStatus(int level, string message)
            {
                model.CurrentProgress = level;
                model.ProgressMessage = message;
                return false;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public SharpGenModel()
        {
        }

        private void RaisePropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        private double progress;

        public double CurrentProgress
        {
            get { return progress; }
            set { progress = value; RaisePropertyChanged(); }
        }

        private string message;

        public string ProgressMessage
        {
            get { return message; }
            set { message = value; RaisePropertyChanged(); }
        }

    }
}
