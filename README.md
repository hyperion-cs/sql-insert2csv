# sql-insert2csv
SQL INSERT statements to CSV converter, written in cross-platform C# .NET (Linux, Windows and macOS supported).

## Why might it be helpful? 🚀
Sometimes we are faced with the need to convert a `.sql` dump (which contains `INSERT` statements) into a `.csv`.

Usually, to do this, we need to import the dump into the DBMS in which it was made.
Then use the export to `.csv` functionality (if it exists at all).

However, this approach has a number of drawbacks:
- You must have an installed instance of a specific DBMS (possibly in a container). This can be overhead, especially if the dumps are from different DBMS or the original DBMS is not known at all;
- Each DBMS requires a different skill to work with it;
- Hard disk overhead required (actual `.sql` dump, imported dump, result in `.csv`).

Now imagine that you are, for example, a data researcher who has to do this kind of procedure on a regular basis. It's really shitty.

This is where `sql-insert2csv` comes in ;)

## Features
- Unified solution for converting `.sql` dumps from any DBMS (~99.9% of variants are supported) to `.csv`;
- No unnecessary hard drive/memory costs (also no intermediate files are made). This utility is capable of processing files with unlimited size (tested ~100GB and larger) in an efficient way;
- The conversion is much faster than through the DBMS directly (because it works in lax mode and without unnecessary checks inherent in a specific DBMS);
- Extremely flexible configuration (see below).

## Getting Started
In progress...

## Configuration
In progress...
