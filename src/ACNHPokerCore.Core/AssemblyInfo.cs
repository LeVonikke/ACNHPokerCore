using System.Runtime.CompilerServices;

// ItemAttr (Inventory/ItemAttr.cs) kept its original `internal` accessibility from the
// WinForms app; this lets the test project exercise it without widening the public API.
[assembly: InternalsVisibleTo("ACNHPokerCore.Core.Tests")]
