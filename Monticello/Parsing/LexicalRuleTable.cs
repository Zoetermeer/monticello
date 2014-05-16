using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monticello.Parsing
{
    public class LexicalRuleTable
    {
        private Dictionary<char, List<LexicalRule>> rules = new Dictionary<char, List<LexicalRule>>();
        private List<LexicalRule> specialRules = new List<LexicalRule>();

        public LexicalRuleTable()
        {

        }

        public void Add(LexicalRule rule)
        {
            if (!rule.StartChar.HasValue)
            {
                //Then this is a 'special' rule, such as an identifier 
                //token matcher
                specialRules.Add(rule);
                return;
            }

            char start = rule.StartChar.GetValueOrDefault();
            List<LexicalRule> rulesForStartChar = null;
            if (!rules.TryGetValue(start, out rulesForStartChar))
            {
                rulesForStartChar = new List<LexicalRule>();
                rules.Add(start, rulesForStartChar);
            }

            rulesForStartChar.Add(rule);
            rulesForStartChar.Sort();
        }

        public void Add(string pat, Sym sym)
        {
            Add(new LexicalRule(pat, sym));
        }

        public IEnumerable<LexicalRule> RulesForStartChar(char c)
        {
            if (rules.ContainsKey(c))
            {
                foreach (var rule in rules[c])
                    yield return rule;
            }

            foreach (var rule in specialRules)
                if (rule.IsPossibleStartChar(c))
                    yield return rule;
        }
    }
}
