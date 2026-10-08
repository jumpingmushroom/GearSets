namespace GearSets.Core.Model
{
    /// <summary>
    /// Valheim's radial (RadialBase.ConstructRadial) moves element 0 of a page that doesn't fill its
    /// ring to the middle of the list and rotates the ring so that element sits at the top; meant
    /// for the back button. A top-level page (the sets opened by the radial key, or the main radial
    /// with the Gear Sets group added) has no back button, so its entries are pre-ordered to come
    /// out with the first one at the top and the rest clockwise in list order.
    /// </summary>
    public static class RadialOrder
    {
        /// <param name="count">Number of entries on the page.</param>
        /// <param name="maxElementsRange">RadialData.SO.MaxElementsRange (ring sizes, e.g. 8 and 12).</param>
        /// <returns>The original index for each list position.</returns>
        public static int[] TopLevel(int count, int[] maxElementsRange)
        {
            var order = new int[count];
            for (int i = 0; i < count; i++)
                order[i] = i;
            if (maxElementsRange == null || maxElementsRange.Length == 0 || count < 2)
                return order;
            int max = maxElementsRange[0];
            for (int i = 0; i < maxElementsRange.Length - 1; i++)
                if (count > maxElementsRange[i])
                    max = maxElementsRange[i + 1];
            if (count >= max)
                return order;
            // Vanilla takes list[0] to index count/2 and shows it at the top; the ones before it
            // end up left of the top (the end of the book), the ones after it to the right.
            int num = count / 2;
            int k = 0;
            order[k++] = 0;
            for (int i = count - num; i < count; i++)
                order[k++] = i;
            for (int i = 1; i < count - num; i++)
                order[k++] = i;
            return order;
        }
    }
}
