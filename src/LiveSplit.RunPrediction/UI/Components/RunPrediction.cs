using LiveSplit.Model;
using LiveSplit.Model.Comparisons;
using LiveSplit.TimeFormatters;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace LiveSplit.UI.Components
{
    public class RunPrediction : IComponent
    {
        protected InfoTimeComponent InternalComponent { get; set; }
        public RunPredictionSettings Settings { get; set; }
        private SplitTimeFormatter Formatter { get; set; }
        private DeltaTimeFormatter DeltaFormatter { get; set; }
        private string PreviousInformationName { get; set; }
        private TimeSpan PriorRunPrediction { get; set; }
        private TimeSpan? PriorRunPredictionDelta { get; set; }
        private int PriorSplitIndex { get; set; }

        public float PaddingTop => InternalComponent.PaddingTop;
        public float PaddingLeft => InternalComponent.PaddingLeft;
        public float PaddingBottom => InternalComponent.PaddingBottom;
        public float PaddingRight => InternalComponent.PaddingRight;

        public IDictionary<string, Action> ContextMenuControls => null; 

        public RunPrediction(LiveSplitState state)
        {
            Settings = new RunPredictionSettings()
            {
                CurrentState = state
            };
            Formatter = new SplitTimeFormatter(Settings.Accuracy);
            DeltaFormatter = new DeltaTimeFormatter();
            PriorRunPrediction = TimeSpan.Zero;
            PriorRunPredictionDelta = null;
            PriorSplitIndex = 0;
            InternalComponent = new InfoTimeComponent(null, null, Formatter);
            state.ComparisonRenamed += state_ComparisonRenamed;
        }

        void state_ComparisonRenamed(object sender, EventArgs e)
        {
            var args = (RenameEventArgs)e;
            if (Settings.Comparison == args.OldName)
            {
                Settings.Comparison = args.NewName;
                ((LiveSplitState)sender).Layout.HasChanged = true;
            }
        }

        private void PrepareDraw(LiveSplitState state)
        {
            InternalComponent.DisplayTwoRows = Settings.Display2Rows;

            InternalComponent.NameLabel.HasShadow 
                = InternalComponent.ValueLabel.HasShadow
                = state.LayoutSettings.DropShadows;

            Formatter.Accuracy = Settings.Accuracy;

            InternalComponent.NameLabel.ForeColor = Settings.OverrideTextColor ? Settings.TextColor : state.LayoutSettings.TextColor;
            InternalComponent.ValueLabel.ForeColor = Settings.OverrideTimeColor ? Settings.TimeColor : state.LayoutSettings.TextColor;
        }

        private void DrawBackground(Graphics g, LiveSplitState state, float width, float height)
        {
            if (Settings.BackgroundColor.A > 0
                || Settings.BackgroundGradient != GradientType.Plain
                && Settings.BackgroundColor2.A > 0)
            {
                var gradientBrush = new LinearGradientBrush(
                            new PointF(0, 0),
                            Settings.BackgroundGradient == GradientType.Horizontal
                            ? new PointF(width, 0)
                            : new PointF(0, height),
                            Settings.BackgroundColor,
                            Settings.BackgroundGradient == GradientType.Plain
                            ? Settings.BackgroundColor
                            : Settings.BackgroundColor2);
                g.FillRectangle(gradientBrush, 0, 0, width, height);
            }
        }

        public void DrawVertical(Graphics g, LiveSplitState state, float width, Region clipRegion)
        {
            DrawBackground(g, state, width, VerticalHeight);
            PrepareDraw(state);
            InternalComponent.DrawVertical(g, state, width, clipRegion);
        }

        public void DrawHorizontal(Graphics g, LiveSplitState state, float height, Region clipRegion)
        {
            DrawBackground(g, state, HorizontalWidth, height);
            PrepareDraw(state);
            InternalComponent.DrawHorizontal(g, state, height, clipRegion);
        }

        public float VerticalHeight => InternalComponent.VerticalHeight;

        public float MinimumWidth => InternalComponent.MinimumWidth;

        public float HorizontalWidth => InternalComponent.HorizontalWidth;

        public float MinimumHeight => InternalComponent.MinimumHeight;

        public string ComponentName => GetDisplayedName(Settings.Comparison);

        public Control GetSettingsControl(LayoutMode mode)
        {
            Settings.Mode = mode;
            return Settings;
        }

        public void SetSettings(System.Xml.XmlNode settings)
        {
            Settings.SetSettings(settings);
        }

        public System.Xml.XmlNode GetSettings(System.Xml.XmlDocument document)
        {
            return Settings.GetSettings(document);
        }

        protected string GetDisplayedName(string comparison)
        {
            switch(comparison)
            {
                case "Current Comparison":
                    return "Current Pace";
                case Run.PersonalBestComparisonName:
                    return "Current Pace";
                case BestSegmentsComparisonGenerator.ComparisonName:
                    return "Best Possible";
                case WorstSegmentsComparisonGenerator.ComparisonName:
                    return "Worst Possible";
                case AverageSegmentsComparisonGenerator.ComparisonName:
                    return "Predicted Time";
                default:
                    return "Current Pace (" + CompositeComparisons.GetShortComparisonName(comparison) + ")";
            }
        }

        protected void SetAlternateText(string comparison)
        {
            switch (comparison)
            {
                case "Current Comparison":
                    InternalComponent.AlternateNameText = new []
                    {
                        "Cur. Pace",
                        "Pace"
                    };
                    break;
                case Run.PersonalBestComparisonName:
                    InternalComponent.AlternateNameText = new []
                    {
                        "Cur. Pace",
                        "Pace"
                    };
                    break;
                case BestSegmentsComparisonGenerator.ComparisonName:
                    InternalComponent.AlternateNameText = new []
                    {
                        "Best Time",
                        "BPT"
                    };
                    break;
                case WorstSegmentsComparisonGenerator.ComparisonName:
                    InternalComponent.AlternateNameText = new []
                    {
                        "Worst Time",
                        "WPT"
                    };
                    break;
                case AverageSegmentsComparisonGenerator.ComparisonName:
                    InternalComponent.AlternateNameText = new []
                    {
                        "Predicted",
                        "Pred."
                    };
                    break;
                default:
                    InternalComponent.AlternateNameText = new []
                    {
                        "Current Pace",
                        "Cur. Pace",
                        "Pace"
                    };
                    break;
            }
        }

        public void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
        {
            var comparison = Settings.Comparison == "Current Comparison" ? state.CurrentComparison : Settings.Comparison;
            if (!state.Run.Comparisons.Contains(comparison))
                comparison = state.CurrentComparison;

            InternalComponent.InformationName = InternalComponent.LongestString = GetDisplayedName(comparison);

            if (InternalComponent.InformationName != PreviousInformationName)
            {
                SetAlternateText(comparison);
                PreviousInformationName = InternalComponent.InformationName;
            }

            if (InternalComponent.InformationName.StartsWith("Current Pace") && state.CurrentPhase == TimerPhase.NotRunning)
            {
                PriorSplitIndex = 0;
                InternalComponent.TimeValue = null;
            }
            else if (state.CurrentPhase == TimerPhase.Running || state.CurrentPhase == TimerPhase.Paused)
            {
                var lastSplit = state.Run.Last().Comparisons[comparison][state.CurrentTimingMethod];
                TimeSpan? lastDelta = LiveSplitStateHelper.GetLastDelta(state, state.CurrentSplitIndex, comparison, state.CurrentTimingMethod);
                var lastDeltaValue = lastDelta ?? TimeSpan.Zero;
                var liveDelta = state.CurrentTime[state.CurrentTimingMethod] - state.CurrentSplit.Comparisons[comparison][state.CurrentTimingMethod];
                if ((PriorSplitIndex == state.CurrentSplitIndex) && (liveDelta > lastDeltaValue))
                {
                    InternalComponent.TimeValue = liveDelta + lastSplit;
                }
                else if ((null != lastDelta) && (null != lastSplit))
                {
                    var runPrediction = lastDeltaValue + lastSplit ?? TimeSpan.Zero;
                    if (PriorSplitIndex != state.CurrentSplitIndex)
                    {
                        if ((PriorSplitIndex < state.CurrentSplitIndex) && (null != state.Run[state.CurrentSplitIndex - 1].SplitTime[state.CurrentTimingMethod]))
                        {
                            PriorRunPredictionDelta = runPrediction - PriorRunPrediction;
                        }
                        else
                        {
                            PriorRunPredictionDelta = null;
                        }
                        PriorRunPrediction = runPrediction;
                        PriorSplitIndex = state.CurrentSplitIndex;
                    }
                    if (null != PriorRunPredictionDelta)
                    {
                        InternalComponent.TimeValue = null;
                        InternalComponent.InformationValue = String.Format("({0}) {1}",
                            DeltaFormatter.Format(PriorRunPredictionDelta), InternalComponent.Formatter.Format(runPrediction));
                    }
                    else
                    {
                        InternalComponent.TimeValue = runPrediction;
                    }
                }
                else
                {
                    PriorSplitIndex = 0;
                    PriorRunPrediction = lastSplit ?? TimeSpan.Zero;
                    PriorRunPredictionDelta = null;
                    InternalComponent.TimeValue = lastSplit;
                }
            }
            else if (state.CurrentPhase == TimerPhase.Ended)
            {
                var finalTime = state.Run.Last().SplitTime[state.CurrentTimingMethod];
                if (PriorSplitIndex > 0)
                {
                    var finalImprovement = finalTime - PriorRunPrediction;
                    InternalComponent.TimeValue = null;
                    InternalComponent.InformationValue = String.Format("({0}) {1}",
                        DeltaFormatter.Format(finalImprovement), InternalComponent.Formatter.Format(finalTime));
                }
                else
                {
                    InternalComponent.TimeValue = finalTime;
                }
            }
            else
            {
                PriorSplitIndex = 0;
                InternalComponent.TimeValue = state.Run.Last().Comparisons[comparison][state.CurrentTimingMethod];
            }

            InternalComponent.Update(invalidator, state, width, height, mode);
        }

        public void Dispose()
        {
        }

        public int GetSettingsHashCode() => Settings.GetSettingsHashCode();
    }
}
