using System;
using System.Collections.Generic;
using Verse;

namespace FamilyTree.FamilyTreeMap
{
    public class GenerationGroup
    {
        public GenerationGroup(Pawn pawn)
        {
            GenerationNumber = (int) pawn.ageTracker.AgeBiologicalYears / 20;
            pawns.Add(pawn);
        }

        public int GenerationNumber;

        private List<Pawn> pawns = new ();

        public bool CanAcceptPawn(Pawn pawn) => GenerationNumber == (int) pawn.ageTracker.AgeBiologicalYears / 20;

        public void AddPawn(Pawn pawn) => pawns.Add(pawn);

        public List<Pawn> GetPawns() => pawns;
    }
}