namespace Assets.Scripts.Game.View.UIAniamtion
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using TMPro;
    using UnityEngine;

    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TypeWriterAnimation : MonoBehaviour
    {
        public TextMeshProUGUI Text;
        public float charsPerSecond = 0.2f; //打字时间间隔
        public bool OnAwakePlay;
        [Tooltip("是否启用打字机效果")] public bool IsEnable = true;
        private bool isActive;
        private float timer; //计时器
        private int currentPos = 0; //当前打字位置
        private Action onFinish;
        private string _currentText;
        private string _finalText; //保存需要显示的文字
        protected static Regex tagPattern = new Regex("<[^>]*>");
        private SortedDictionary<int, Match> tmpdict = new SortedDictionary<int, Match>();
        private string _totalText; //所有文字
        private int lastindex = 0;
        private int reset = 0;
        private static SortedDictionary<int,Match> repeatItems = new SortedDictionary<int, Match>();
        public bool IsPlaying => isActive;

        void Awake()
        {
            if (OnAwakePlay)
            {
                Init(Text.text);
            }
        }

        public void Init(string str, Action callback = null)
        {
            Init(str, 0, str.Length, callback);
        }

        public void Init(string str, int index, int len, Action callback = null)
        {
            timer = 0;
            _totalText = str;
            isActive = true;
            // charsPerSecond = Mathf.Max(0.2f, charsPerSecond);
            Text = GetComponent<TextMeshProUGUI>();
            this.SubLitStr(str, index, len);
            _finalText = ReplaceStr(str).Substring(index, len);
            _currentText = _finalText;
            if (!IsEnable)
            {
                OnFinish();
            }
            else
            {
                Text.text = ""; //获取Text的文本信息，保存到words中，然后动态更新文本显示内容，实现打字机的效果
                if (callback != null)
                    onFinish = callback;
            }
        }

        /// <summary>
        /// 处理富文本
        /// </summary>
        /// <param name="content"></param>
        /// <param name="index"></param>
        /// <param name="len"></param>
        public void SubLitStr(string content, int index, int len)
        {
            foreach (var match in tmpdict)
            {
                if (repeatItems.ContainsKey(match.Key))
                {
                    repeatItems.Remove(match.Key);
                }
            }
            tmpdict.Clear();
            tmpdict = SubLitString(content, index, len, ref tmpdict);
        }

        public void Play()
        {
            isActive = true;
        }

        public void Pause()
        {
            isActive = false;
        }

        public void Stop()
        {
            OnFinish();
        }


        private void Update()
        {
            if (!IsEnable) return;
            OnStartWriter();
        }


        /// <summary>
        /// 执行打字任务
        /// </summary>
        private void OnStartWriter()
        {
            if (isActive)
            {
                timer += Time.deltaTime;
                if (timer >= charsPerSecond)
                {
                    //判断计时器时间是否到达
                    timer = 0;
                    int len = _currentText.Length;
                    int repeat = 0;
                    string endcode = String.Empty;
                    int finallen = 0;
                    if (currentPos < len)
                    {
                        List<Match> tmplist = tmpdict.Values.ToList();
                        for (int i = 0; i < tmplist.Count; i ++)
                        {
                            var item1 = tmplist[i];
                            if (i+1 >= tmplist.Count)
                            {
                                break;
                            }
                            var item2 = tmplist[i + 1];
                            if (currentPos == item1.Index - lastindex + reset)
                            {
                                finallen = item1.Index - lastindex;
                                if (item1.Index - lastindex > _currentText.Length)
                                {
                                    finallen = _currentText.Length;
                                }
                                _currentText = _currentText.Insert(finallen+reset, item1.Value);
                                currentPos += item1.Length;
                                endcode += item2.Value;
                            }
                            if (currentPos == item2.Index - lastindex + reset)
                            {
                                finallen = item2.Index - lastindex;
                                if (item2.Index - lastindex > _currentText.Length)
                                {
                                    finallen = _currentText.Length;
                                }
                                _currentText = _currentText.Insert(finallen+reset, item2.Value);
                                currentPos += item2.Length;
                                endcode = String.Empty;
                            }
                            else if(item1.Index - lastindex < 0 && currentPos == reset)
                            {
                                _currentText = _currentText.Insert(reset, item1.Value);
                                currentPos += item1.Length;
                                ++repeat;
                                reset = currentPos;
                            }
                        }
                        for (int i = 0; i < repeat; i++)
                        {
                            endcode += tmplist[i+repeat].Value;
                        }
                    }
                    currentPos++;
                    int curpos = currentPos > _currentText.Length ? _currentText.Length : currentPos;
                    string content = String.Format("{0}{1}", _currentText.Substring(0, curpos), endcode);
                    Text.text = content; //刷新文本显示内容
                    if (currentPos >= _currentText.Length)
                    {
                        OnFinish();
                    }
                }
            }
        }

        /// <summary>
        /// 结束打字，初始化数据
        /// </summary>
        private void OnFinish()
        {
            isActive = false;
            timer = 0;
            currentPos = 0;
            reset = 0;
            _currentText = _finalText;
            int firstlen = 0;
            foreach (var it in tmpdict)
            {
                int index = it.Value.Index - lastindex;
                if (it.Value.Index - lastindex < 0)
                {
                    firstlen++;
                    continue;
                }
                else if (it.Value.Index - lastindex > _currentText.Length)
                {
                    index = _currentText.Length;
                }
                _currentText = _currentText.Insert(index, it.Value.Value);
            }
            if (tmpdict.Count > 0 &&　firstlen > 0)
            {
                var list = tmpdict.Values.ToList();
                var keys = tmpdict.Keys.ToList();
                var first = tmpdict.Values.First();
                var firstkey = tmpdict.Keys.First();
                _currentText = _currentText.Insert(0, first.Value);
                for (int i = 1; i < firstlen; i++)
                {
                    _currentText = _currentText.Insert(first.Length, list[i].Value);
                    first = list[i];
                }
            }
            ResetText();
            Text.text = _currentText;
            onFinish?.Invoke();
        }
        //重置上一次截断位置索引
        private void ResetText()
        {
            int lastlen = 0;
            int len = 0;
            foreach (var kMatch in repeatItems)
            {
                lastlen += kMatch.Value.Length;
                if (kMatch.Value.Value.Contains("</"))
                {
                    len += kMatch.Value.Length;
                }
            }
            lastindex += _currentText.Length - lastlen;
            if (lastindex >= _totalText.Length - len)
            {
                lastindex = 0;
                repeatItems.Clear();
            }
        }


        /// <summary>
        /// 把富文本标签移除掉
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string ReplaceStr(string text)
        {
            var result = "";
            Regex regex = tagPattern;
            result = regex.Replace(text, "");
            return result;
        }

        /// <summary>
        /// 查找第一项匹配的字符串
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static MatchCollection FindReplace(string text)
        {
            Regex regex = tagPattern;
            var match = regex.Matches(text);
            return match;
        }

        
        public static SortedDictionary<int, Match> SubLitString(string content, int startindex, int len,
            ref SortedDictionary<int, Match> tmp)
        {
            int index = 0;
            int totallen = 0;
            var match = FindReplace(content);
            Match oldvalue = null;
            if (match.Count > 0)
            {
                oldvalue = match[0];
            }

            for (int i = 0; i < match.Count; i++)
            {
                index = match[i].Index;
                if (i > 0)
                {
                    totallen += oldvalue.Length;
                    index -= totallen;
                }

                if (index >= startindex && index < startindex + len)
                {
                    tmp.Add(i, match[i]);
                }
                oldvalue = match[i];
            }
            if (tmp.Count > 0)
            {
                int Startlen = 0;
                int endlen = 0;
                foreach (var it in tmp)
                {
                    Match value = it.Value;
                    if (value.Value.Contains("</"))
                    {
                        Startlen++;
                    }
                    else
                    {
                        break;
                    }
                }
                List<Match> values = tmp.Values.ToList();
                for (int i = values.Count-1; i >= 0; i--)
                {
                    Match value = values[i];
                    if (!value.Value.Contains("</"))
                    {
                        endlen++;
                    }
                    else
                    {
                        break;
                    }
                }
                int firstkey = tmp.Keys.First();
                int endkey = tmp.Keys.Last();
                for (int i = 1; i <= Startlen; i++)
                {
                    tmp.Add(firstkey-i,match[firstkey-i]);
                    if (!repeatItems.ContainsKey(firstkey - i))
                    {
                        repeatItems.Add(firstkey - i,match[firstkey-i]);
                    }
                }
                for (int i = 1; i <= endlen; i++)
                {
                    tmp.Add(endkey+i,match[endkey+i]);
                    if (!repeatItems.ContainsKey(endkey + i))
                    {
                        repeatItems.Add(endkey + i,match[endkey + i]);
                    }
                }
            }
            return tmp;
        }
    }
}