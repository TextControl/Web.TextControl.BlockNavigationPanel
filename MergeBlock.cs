using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using TXTextControl;
using TXTextControl.DocumentServer.Fields;

namespace tx_sample_html5
{
    public class MergeBlock
    {

        private List<MergeField> _fields;    // Fields inside this merge block
        private List<MergeBlock> _children;    // Children
        private MergeBlock _parent = null;

        public const string BlockStartPrefix = "BlockStart_";
        public const string BlockEndPrefix = "BlockEnd_";

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="startMarker">Block start marker.</param>
        /// <param name="endMarker">Block end marker.</param>
        public MergeBlock(TXTextControl.DocumentTarget startMarker, TXTextControl.DocumentTarget endMarker)
        {
            StartMarker = startMarker;
            EndMarker = endMarker;
            _fields = new List<MergeField>();
            _children = new List<MergeBlock>();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public MergeBlock(List<DocumentTarget> blockMarkers, ref int i, ServerTextControl textControl, MergeBlock parent)
        {
            if (!blockMarkers[i].TargetName.StartsWith(BlockStartPrefix))
            {
                throw new Exception(
                   "Invalid merge block structure. Block start marker doesn't start with the block start prefix (Marker name: \""
                   + blockMarkers[i].TargetName + "\")");
            }

            _fields = new List<MergeField>();
            _children = new List<MergeBlock>();
            _parent = parent;

            StartMarker = blockMarkers[i++];

            // Gather child blocks if any
            while ((i < blockMarkers.Count)
               && blockMarkers[i].TargetName.StartsWith(BlockStartPrefix))
            {
                _children.Add(new MergeBlock(blockMarkers, ref i, textControl, this));
                ++i;
            }

            if (i >= blockMarkers.Count)
            {   // Not possible
                throw new Exception("Invalid merge block structure.");
            }

            // End marker.
            string endMarkerName = blockMarkers[i].TargetName.Substring(BlockEndPrefix.Length);
            if (endMarkerName != Name)
            {
                throw new Exception(
                   "Invalid merge block structure. End marker name is not the same as the start marker name. (End marker name: \""
                   + blockMarkers[i].TargetName + "\")");
            }
            EndMarker = blockMarkers[i];

            // Get fields inside this block but not in child blocks
            GetFields(textControl);
        }

        /// <summary>
        /// Gets the block name by removing the block name prefix from 
        /// the start marker's name.
        /// </summary>
        public string Name
        {
            get
            {
                if (StartMarker == null) return string.Empty;
                return StartMarker.TargetName.Substring(BlockStartPrefix.Length);
            }

            set
            {
                if ((StartMarker == null) || (EndMarker == null)) return;
                if (string.IsNullOrEmpty(value))
                {
                    throw new Exception("Block name must not be null or empty.");
                }

                StartMarker.TargetName = BlockStartPrefix + value;
                EndMarker.TargetName = BlockEndPrefix + value;
            }
        }

        /// <summary>
        /// Fields inside this merge block excluding fields in nested blocks.
        /// </summary>
        public IList<MergeField> Fields
        {
            get { return new ReadOnlyCollection<MergeField>(_fields); }
        }

        /// <summary>
        /// Gets or sets the start marker of this merge block.
        /// </summary>
        public TXTextControl.DocumentTarget StartMarker { get; private set; }

        /// <summary>
        /// Gets or sets the end marker of this merge block.
        /// </summary>
        public TXTextControl.DocumentTarget EndMarker { get; private set; }

        /// <summary>
        /// Checks if a text position lies between the block's start and end markers.
        /// </summary>
        /// <param name="nTextPos">The text position.</param>
        /// <returns>The text position is inside the block.</returns>
        private bool Contains(int nTextPos)
        {
            if ((StartMarker == null) || (EndMarker == null)) return false;
            return ((nTextPos >= StartMarker.Start) && (nTextPos <= EndMarker.Start));
        }

        /// <summary>
        /// Gets the block's length in characters.
        /// </summary>
        public int Length
        {
            get
            {
                if ((StartMarker == null) || (EndMarker == null)) return 0;
                return EndMarker.Start - StartMarker.Start;
            }
        }

        /// <summary>
        /// Gets merge blocks nested in this merge block.
        /// </summary>
        public IList<MergeBlock> Children
        {
            get { return new ReadOnlyCollection<MergeBlock>(_children); }
        }

        /// <summary>
        /// Returns a merge block containing a given text position in a list of merge blocks. Returns the
        /// innermost nested block in case the text position lies inside nested blocks.
        /// </summary>
        /// <param name="blocks">List of merge blocks.</param>
        /// <param name="nTextPos">Text position</param>
        /// <returns>Block containing the text position or null.</returns>
        public static MergeBlock FindBlockContainingTextPos(List<MergeBlock> blocks, int nTextPos)
        {
            // Get all merge blocks containing the given text position
            List<MergeBlock> candidates = blocks.FindAll(block => block.Contains(nTextPos));

            if (candidates.Count == 0) return null;
            if (candidates.Count == 1) return candidates[0];

            // Sort blocks according to their length
            candidates.Sort(MergeBlock.SortLength);

            // Return the shortest, i. e. the innermost nested block which contains the text position
            return candidates[0];
        }

        /// <summary>
        /// Returns IComparer comparing block lengths.
        /// </summary>
        /// <returns></returns>
        public static IComparer<MergeBlock> SortLength
        {
            get { return (IComparer<MergeBlock>)new MergeBlockLengthComparer(); }
        }

        /// <summary>
        /// Returns IComparer comparing block positions.
        /// </summary>
        /// <returns></returns>
        public static IComparer<MergeBlock> SortPosition
        {
            get { return (IComparer<MergeBlock>)new MergeBlockPositionComparer(); }
        }

        /// <summary>
        /// Makes merge blocks from a valid and sorted list of block markers. (Check 
        /// with ValidateMergeBlockNesting beforehand and get list with GetBlockMarkersOrdered)
        /// </summary>
        public static List<MergeBlock> GetMergeBlocks(List<DocumentTarget> blockMarkers, ServerTextControl textControl)
        {
            var result = new List<MergeBlock>();
            if (blockMarkers.Count == 0) return result;

            if (!blockMarkers[0].TargetName.StartsWith(BlockStartPrefix))
            {
                throw new Exception("MergeBlock.GetMergeBlocks: invalid merge block structure.");
            }

            for (int i = 0; i < blockMarkers.Count; ++i)
            {
                result.Add(new MergeBlock(blockMarkers, ref i, textControl, null));
            }

            return result;
        }

        private void GetFields(ServerTextControl textControl)
        {
            var fields = new List<MergeField>();
            foreach (ApplicationField appFld in textControl.ApplicationFields)
            {
                int nFldEnd = appFld.Start + appFld.Length;
                if ((appFld.Start >= StartMarker.Start) && (nFldEnd <= EndMarker.Start))
                {
                    MergeField adap = MakeAdapterFrom(appFld);
                    if (adap != null) fields.Add(adap);
                }
            }

            foreach (MergeField fld in fields)
            {
                if (!IsInside(fld.ApplicationField, _children)) _fields.Add(fld);
            }
        }

        /// <summary>
        /// Checks if an application field lies inside any block in a list of merge blocks.
        /// </summary>
        public static bool IsInside(ApplicationField field, List<MergeBlock> blocks)
        {
            foreach (var block in blocks)
            {
                if (IsInside(field, block)) return true;
            }

            return false;
        }

        private static bool IsInside(ApplicationField field, MergeBlock block)
        {
            int nFldEnd = field.Start + field.Length;
            return (block.StartMarker.Start <= field.Start) && (block.EndMarker.Start >= nFldEnd);
        }

        /// <summary>
        /// Makes a mailmerge field adapter from an application field if possible.
        /// Returns null if field is not a MERGEFIELD.
        /// </summary>
        internal static MergeField MakeAdapterFrom(ApplicationField fld)
        {
            switch (fld.TypeName)
            {
                case MergeField.TYPE_NAME:
                    return new MergeField(fld);
            }

            return null;
        }

        /// <summary>
        /// Returns a list of all block start and end markers ordered by their start position.
        /// </summary>
        /// <returns>Sorted list of block markers.</returns>
        public static List<TXTextControl.DocumentTarget> GetBlockMarkersOrdered(ServerTextControl textControl)
        {
            var targets = new List<TXTextControl.DocumentTarget>();
            foreach (TXTextControl.DocumentTarget tgt in textControl.DocumentTargets) targets.Add(tgt);

            var blockMarkers = new List<TXTextControl.DocumentTarget>();
            foreach (TXTextControl.DocumentTarget tgt in targets)
            {
                if (tgt.TargetName.StartsWith(MergeBlock.BlockStartPrefix, StringComparison.OrdinalIgnoreCase)
                   || tgt.TargetName.StartsWith(MergeBlock.BlockEndPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    blockMarkers.Add(tgt);
                }
            }

            blockMarkers.Sort(new DocumentTargetComparer());
            return blockMarkers;
        }

        /// <summary>
        /// Validates merge block nesting
        /// </summary>
        public static void ValidateMergeBlockNesting(ServerTextControl textControl)
        {
            // Get a list of all block start and end markers ordered by their start position:
            var blockMarkers = GetBlockMarkersOrdered(textControl);
            var startMarkers = new Stack<TXTextControl.DocumentTarget>();

            foreach (var marker in blockMarkers)
            {
                if (marker.TargetName.StartsWith(MergeBlock.BlockStartPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    // Start marker:
                    startMarkers.Push(marker);
                }
                else if (marker.TargetName.StartsWith(MergeBlock.BlockEndPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    // End marker:
                    var endMarkerName = marker.TargetName.Substring(MergeBlock.BlockEndPrefix.Length);  // Get block name
                    TXTextControl.DocumentTarget startMarker = (startMarkers.Count > 0) ? startMarkers.Pop() : null;   // Get currently open start marker
                    var startMarkerName
                       = (startMarker != null)
                       ? startMarker.TargetName.Substring(MergeBlock.BlockStartPrefix.Length)
                       : string.Empty;

                    if (endMarkerName.ToLower() != startMarkerName.ToLower())
                    {
                        if (startMarkerName != string.Empty)
                        {
                            throw new Exception(
                               "Block nesting invalid somewhere after block start marker “"
                               + MergeBlock.BlockStartPrefix + startMarkerName + "”.");
                        }
                        else
                        {
                            throw new Exception(
                               "Invalid block end marker “"
                               + MergeBlock.BlockEndPrefix + endMarkerName + "”.");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gathers merge blocks in current document.
        /// </summary>
        /// <returns>Merge blocks in current document.</returns>
        public static List<MergeBlock> GetMergeBlocksFlattened(ServerTextControl textControl)
        {
            ValidateMergeBlockNesting(textControl);
            var result = new List<MergeBlock>();

            // Get a list of all block start and end markers ordered by their start position:
            var blockMarkers = GetBlockMarkersOrdered(textControl);

            // Gather blocks:

            var startMarkers = new Stack<TXTextControl.DocumentTarget>();

            foreach (var marker in blockMarkers)
            {
                if (marker.TargetName.StartsWith(MergeBlock.BlockStartPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    // Start marker:
                    startMarkers.Push(marker);
                }
                else if (marker.TargetName.StartsWith(MergeBlock.BlockEndPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    // End marker:
                    TXTextControl.DocumentTarget startMarker = startMarkers.Pop();   // Get currently open start marker
                    var blockNew = new MergeBlock(startMarker, marker);
                    result.Add(blockNew);
                }
            }

            result.Sort(MergeBlock.SortPosition);
            return result;
        }

    } // class MergeBlock

    class DocumentTargetComparer : IComparer<TXTextControl.DocumentTarget>
    {

        #region IComparer<DocumentTarget> Members

        public int Compare(TXTextControl.DocumentTarget x, TXTextControl.DocumentTarget y)
        {
            return x.Start - y.Start;
        }

        #endregion

    }

    public class MergeBlockPositionComparer : IComparer<MergeBlock>
    {

        int IComparer<MergeBlock>.Compare(MergeBlock x, MergeBlock y)
        {
            return x.StartMarker.Start - y.StartMarker.Start;
        }

    } // class MergeBlockPositionComparer

    public class MergeBlockLengthComparer : IComparer<MergeBlock>
    {

        #region IComparer<MergeBlock> Members

        int IComparer<MergeBlock>.Compare(MergeBlock x, MergeBlock y)
        {
            return x.Length - y.Length;
        }

        #endregion

    } // class MergeBlockLengthComparer
}