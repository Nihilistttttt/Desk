using System;
using System.Windows;
using System.Windows.Controls;
using GeekDesk.ViewModel;

namespace GeekDesk.CustomComponent.GridPositionPanel
{
    public class GridPositionPanel : Panel
    {
        public static readonly DependencyProperty CellWidthProperty =
            DependencyProperty.Register(nameof(CellWidth), typeof(double), typeof(GridPositionPanel),
                new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

        public static readonly DependencyProperty CellHeightProperty =
            DependencyProperty.Register(nameof(CellHeight), typeof(double), typeof(GridPositionPanel),
                new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

        public static readonly DependencyProperty ColumnsProperty =
            DependencyProperty.Register(nameof(Columns), typeof(int), typeof(GridPositionPanel),
                new FrameworkPropertyMetadata(13, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public double CellWidth
        {
            get => (double)GetValue(CellWidthProperty);
            set => SetValue(CellWidthProperty, value);
        }

        public double CellHeight
        {
            get => (double)GetValue(CellHeightProperty);
            set => SetValue(CellHeightProperty, value);
        }

        public int Columns
        {
            get => (int)GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            double maxCol = 0;
            double maxRow = 0;

            foreach (UIElement child in InternalChildren)
            {
                child.Measure(new Size(CellWidth, CellHeight));

                if (child is FrameworkElement fe && fe.DataContext is IconInfo icon)
                {
                    if (icon.GridX >= 0 && icon.GridY >= 0)
                    {
                        maxCol = Math.Max(maxCol, icon.GridX + 1);
                        maxRow = Math.Max(maxRow, icon.GridY + 1);
                    }
                }
            }

            double width = Math.Max(maxCol, Columns) * CellWidth;
            double height = Math.Max(maxRow * CellHeight, 0);
            return new Size(Math.Max(width, availableSize.Width), height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (UIElement child in InternalChildren)
            {
                if (child is FrameworkElement fe && fe.DataContext is IconInfo icon)
                {
                    if (icon.GridX >= 0 && icon.GridY >= 0)
                    {
                        double x = icon.GridX * CellWidth;
                        double y = icon.GridY * CellHeight;
                        child.Arrange(new Rect(x, y, CellWidth, CellHeight));
                    }
                    else
                    {
                        child.Arrange(new Rect(0, 0, 0, 0));
                    }
                }
            }
            return finalSize;
        }
    }
}