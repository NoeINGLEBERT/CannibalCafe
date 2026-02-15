using System.Collections.Generic;

public static class FamilialLogic
{
    private static readonly Dictionary<FamilialRelationType, Dictionary<FamilialRelationType, FamilialRelationType>> grid = new()
    {
        [FamilialRelationType.Unspecified] = new() // CORRECT
        {
            [FamilialRelationType.Unspecified] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Parent] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Child] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Grandparent] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Grandchild] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Sibling] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Avuncular] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Nibling] = FamilialRelationType.Unspecified,
            [FamilialRelationType.GrandAvuncular] = FamilialRelationType.Unspecified,
            [FamilialRelationType.GrandNibling] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Cousin] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Unrelated] = FamilialRelationType.Unrelated,
        },
        [FamilialRelationType.Parent] = new() // CORRECT
        {
            [FamilialRelationType.Unspecified] = FamilialRelationType.Unspecified, // A Parent of B, B Unspecified to C, A Unspecified to C
            [FamilialRelationType.Parent] = FamilialRelationType.Grandparent, // A Parent of B, B Parent of C, A Grandparent of C
            [FamilialRelationType.Child] = FamilialRelationType.Unrelated, // A Parent of B, B Child of C, A Unrelated to C (Mother/Father relation)
            [FamilialRelationType.Grandparent] = FamilialRelationType.Unspecified, // A Parent of B, B Grandparent of C, A Unspecified to C (Great Grandparent relation)
            [FamilialRelationType.Grandchild] = FamilialRelationType.Unspecified, // A Parent of B, B Grandchild of C, A Unspecified to C (Child or Unrelated relation)
            [FamilialRelationType.Sibling] = FamilialRelationType.Parent, // A Parent of B, B Sibling of C, A Parent of C
            [FamilialRelationType.Avuncular] = FamilialRelationType.Grandparent, // A Parent of B, B Avuncular of C, A Grandparent of C
            [FamilialRelationType.Nibling] = FamilialRelationType.Sibling, // A Parent of B, B Nibling of C, A Sibling of C
            [FamilialRelationType.GrandAvuncular] = FamilialRelationType.Unspecified, // A Parent of B, B GrandAvuncular of C, A Unspecified to C
            [FamilialRelationType.GrandNibling] = FamilialRelationType.Nibling, // A Parent of B, B GrandNibling of C, A Unspecified to C
            [FamilialRelationType.Cousin] = FamilialRelationType.Avuncular, // A Parent of B, B Cousin of C, A Avuncular of C
            [FamilialRelationType.Unrelated] = FamilialRelationType.Unrelated, // A Parent of B, B Unrelated to C, A Unrelated to C
        },
        [FamilialRelationType.Child] = new() // TO CHECK & COMMENT
        {
            [FamilialRelationType.Unspecified] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Parent] = FamilialRelationType.Sibling,
            [FamilialRelationType.Child] = FamilialRelationType.Grandchild,
            [FamilialRelationType.Grandparent] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Grandchild] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Sibling] = FamilialRelationType.Avuncular,
            [FamilialRelationType.Avuncular] = FamilialRelationType.Cousin,
            [FamilialRelationType.Nibling] = FamilialRelationType.GrandAvuncular,
            [FamilialRelationType.GrandAvuncular] = FamilialRelationType.Unspecified,
            [FamilialRelationType.GrandNibling] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Cousin] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Unrelated] = FamilialRelationType.Unrelated,
        },
        [FamilialRelationType.Grandparent] = new() // TO DO
        {
            [FamilialRelationType.Unspecified] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Parent] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Child] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Grandparent] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Grandchild] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Sibling] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Avuncular] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Nibling] = FamilialRelationType.Unspecified,
            [FamilialRelationType.GrandAvuncular] = FamilialRelationType.Unspecified,
            [FamilialRelationType.GrandNibling] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Cousin] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Unrelated] = FamilialRelationType.Unrelated,
        },
        [FamilialRelationType.Grandchild] = new() // TO DO
        {
            [FamilialRelationType.Unspecified] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Parent] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Child] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Grandparent] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Grandchild] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Sibling] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Avuncular] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Nibling] = FamilialRelationType.Unspecified,
            [FamilialRelationType.GrandAvuncular] = FamilialRelationType.Unspecified,
            [FamilialRelationType.GrandNibling] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Cousin] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Unrelated] = FamilialRelationType.Unrelated,
        },
        [FamilialRelationType.Sibling] = new() // TO CHECK & COMMENT
        {
            [FamilialRelationType.Unspecified] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Parent] = FamilialRelationType.Avuncular,
            [FamilialRelationType.Child] = FamilialRelationType.Child,
            [FamilialRelationType.Grandparent] = FamilialRelationType.GrandAvuncular,
            [FamilialRelationType.Grandchild] = FamilialRelationType.Grandchild,
            [FamilialRelationType.Sibling] = FamilialRelationType.Sibling,
            [FamilialRelationType.Avuncular] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Nibling] = FamilialRelationType.Nibling,
            [FamilialRelationType.GrandAvuncular] = FamilialRelationType.Unspecified,
            [FamilialRelationType.GrandNibling] = FamilialRelationType.GrandNibling,
            [FamilialRelationType.Cousin] = FamilialRelationType.Cousin,
            [FamilialRelationType.Unrelated] = FamilialRelationType.Unrelated,
        },
        [FamilialRelationType.Avuncular] = new() // TO DO
        {
            [FamilialRelationType.Unspecified] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Parent] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Child] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Grandparent] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Grandchild] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Sibling] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Avuncular] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Nibling] = FamilialRelationType.Unspecified,
            [FamilialRelationType.GrandAvuncular] = FamilialRelationType.Unspecified,
            [FamilialRelationType.GrandNibling] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Cousin] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Unrelated] = FamilialRelationType.Unrelated,
        },
        [FamilialRelationType.Nibling] = new() // TO DO
        {
            [FamilialRelationType.Unspecified] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Parent] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Child] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Grandparent] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Grandchild] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Sibling] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Avuncular] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Nibling] = FamilialRelationType.Unspecified,
            [FamilialRelationType.GrandAvuncular] = FamilialRelationType.Unspecified,
            [FamilialRelationType.GrandNibling] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Cousin] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Unrelated] = FamilialRelationType.Unrelated,
        },
        [FamilialRelationType.GrandAvuncular] = new() // TO DO
        {
            [FamilialRelationType.Unspecified] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Parent] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Child] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Grandparent] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Grandchild] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Sibling] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Avuncular] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Nibling] = FamilialRelationType.Unspecified,
            [FamilialRelationType.GrandAvuncular] = FamilialRelationType.Unspecified,
            [FamilialRelationType.GrandNibling] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Cousin] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Unrelated] = FamilialRelationType.Unrelated,
        },
        [FamilialRelationType.GrandNibling] = new() // TO DO
        {
            [FamilialRelationType.Unspecified] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Parent] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Child] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Grandparent] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Grandchild] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Sibling] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Avuncular] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Nibling] = FamilialRelationType.Unspecified,
            [FamilialRelationType.GrandAvuncular] = FamilialRelationType.Unspecified,
            [FamilialRelationType.GrandNibling] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Cousin] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Unrelated] = FamilialRelationType.Unrelated,
        },
        [FamilialRelationType.Cousin] = new() // TO DO
        {
            [FamilialRelationType.Unspecified] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Parent] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Child] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Grandparent] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Grandchild] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Sibling] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Avuncular] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Nibling] = FamilialRelationType.Unspecified,
            [FamilialRelationType.GrandAvuncular] = FamilialRelationType.Unspecified,
            [FamilialRelationType.GrandNibling] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Cousin] = FamilialRelationType.Unspecified,
            [FamilialRelationType.Unrelated] = FamilialRelationType.Unrelated,
        },
        [FamilialRelationType.Unrelated] = new() // CORRECT
        {
            [FamilialRelationType.Unspecified] = FamilialRelationType.Unrelated,
            [FamilialRelationType.Parent] = FamilialRelationType.Unrelated,
            [FamilialRelationType.Child] = FamilialRelationType.Unrelated,
            [FamilialRelationType.Grandparent] = FamilialRelationType.Unrelated,
            [FamilialRelationType.Grandchild] = FamilialRelationType.Unrelated,
            [FamilialRelationType.Sibling] = FamilialRelationType.Unrelated,
            [FamilialRelationType.Avuncular] = FamilialRelationType.Unrelated,
            [FamilialRelationType.Nibling] = FamilialRelationType.Unrelated,
            [FamilialRelationType.GrandAvuncular] = FamilialRelationType.Unrelated,
            [FamilialRelationType.GrandNibling] = FamilialRelationType.Unrelated,
            [FamilialRelationType.Cousin] = FamilialRelationType.Unrelated,
            [FamilialRelationType.Unrelated] = FamilialRelationType.Unrelated,
        },
    };

    public static FamilialRelationType Resolve(FamilialRelationType myRelation, FamilialRelationType theirRelation)
    {
        if (!grid.TryGetValue(myRelation, out var row)) return FamilialRelationType.Unspecified;
        if (!row.TryGetValue(theirRelation, out var result)) return FamilialRelationType.Unspecified;
        return result;
    }
}