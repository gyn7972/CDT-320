using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace QMC.Vision.Ui.Controls
{
    /// <summary>
    /// DataGridView 컬럼 헤더 클릭 정렬을 지원하는 BindingList.
    /// 기본 BindingList 는 정렬 미지원이라, 헤더 클릭 시 자동 정렬되도록 ApplySortCore 를 구현.
    /// </summary>
    public class SortableBindingList<T> : BindingList<T>
    {
        private bool _sorted;
        private ListSortDirection _dir;
        private PropertyDescriptor _prop;

        public SortableBindingList() { }
        public SortableBindingList(IEnumerable<T> items)
        {
            if (items != null) foreach (var i in items) Add(i);
        }

        protected override bool SupportsSortingCore => true;
        protected override bool IsSortedCore => _sorted;
        protected override ListSortDirection SortDirectionCore => _dir;
        protected override PropertyDescriptor SortPropertyCore => _prop;

        protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
        {
            _prop = prop; _dir = direction;
            var items = Items as List<T>;
            if (items == null) return;
            items.Sort((a, b) =>
            {
                object va = prop.GetValue(a), vb = prop.GetValue(b);
                int cmp;
                if (va == null && vb == null) cmp = 0;
                else if (va == null) cmp = -1;
                else if (vb == null) cmp = 1;
                else if (va is IComparable ca && va.GetType() == vb.GetType()) cmp = ca.CompareTo(vb);
                else cmp = string.Compare(va.ToString(), vb.ToString(), StringComparison.OrdinalIgnoreCase);
                return direction == ListSortDirection.Descending ? -cmp : cmp;
            });
            _sorted = true;
            OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
        }

        protected override void RemoveSortCore() { _sorted = false; _prop = null; }
    }
}
