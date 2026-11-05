using System;

namespace AIAdventure.Level
{
    /// <summary>
    /// Rakamlı şifre kilidi. Unity'den bağımsız saf C#.
    /// Her hane 0-9 arasında döner; doğru sıra girilince açılır.
    /// </summary>
    public class CombinationLock
    {
        private readonly int[] code;
        private readonly int[] digits;

        public int Length => digits.Length;
        public int Selected { get; private set; }
        public int Attempts { get; private set; }

        public CombinationLock(string codeText)
        {
            if (string.IsNullOrEmpty(codeText)) codeText = "000";
            code = new int[codeText.Length];
            digits = new int[codeText.Length];
            for (int i = 0; i < codeText.Length; i++)
                code[i] = char.IsDigit(codeText[i]) ? codeText[i] - '0' : 0;
        }

        public int DigitAt(int index) => digits[index];

        /// <summary>Seçili haneyi yukarı (+1) ya da aşağı (-1) çevirir.</summary>
        public void Rotate(int direction)
        {
            digits[Selected] = (digits[Selected] + Math.Sign(direction) + 10) % 10;
        }

        /// <summary>Sağdaki ya da soldaki haneye geçer.</summary>
        public void MoveSelection(int direction)
        {
            Selected = (Selected + Math.Sign(direction) + digits.Length) % digits.Length;
        }

        /// <summary>Şifreyi dener. Doğruysa true döner.</summary>
        public bool TryOpen()
        {
            Attempts++;
            for (int i = 0; i < code.Length; i++)
                if (digits[i] != code[i]) return false;
            return true;
        }
    }
}
