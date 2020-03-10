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
        private Dictionary<int, Match> tmpdict;
        public bool IsPlaying => isActive;

        void Awake()
        {
            if (OnAwakePlay)
            {
                Init(Text.text);
            }
            tmpdict = new Dictionary<int, Match>();
        }

        public void Init(string str, Action callback = null)
        {
            timer = 0;
            isActive = true;
            charsPerSecond = Mathf.Max(0.2f, charsPerSecond);
            Text = GetComponent<TextMeshProUGUI>();
            _finalText = str;
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
        public void SubLitStr(string content,int index,int len)
        {
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
                    currentPos++;
                    int len = _currentText.Length;
                    string endcode = String.Empty;
                    if (currentPos < len)
                    {
                        List<Match> tmplist = tmpdict.Values.ToList();
                        for (int i = 0; i < tmplist.Count; i+=2)
                        {
                            var item1 = tmplist[i];
                            var item2 = tmplist[i+1];
                            int finallen = 0;
                            if (currentPos == item1.Index)
                            {
                                finallen = item1.Index;
                                if (item1.Index > _currentText.Length)
                                {
                                    finallen = _currentText.Length;
                                }
                                _currentText = _currentText.Insert(finallen, item1.Value);
                                currentPos += item1.Length;
                                endcode = item2.Value;
                                break;
                            }
                            if (currentPos == item2.Index)
                            {
                                finallen = item2.Index;
                                if (item2.Index > _currentText.Length)
                                {
                                    finallen = _currentText.Length;
                                }
                                _currentText = _currentText.Insert(finallen, item2.Value);
                                currentPos += item2.Length;
                                endcode = String.Empty;
                            }
                        }
                    }
                    int curpos = currentPos > _currentText.Length ? _currentText.Length : currentPos;
                    string content = String.Format("{0}{1}",_currentText.Substring(0, curpos),endcode);
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
            _currentText = _finalText;
            foreach (var it in tmpdict)
            {
                _currentText = _currentText.Insert(it.Value.Index, it.Value.Value);
            }
            Text.text = _currentText;
            onFinish?.Invoke();
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

        public static Dictionary<int, Match> SubLitString(string content, int startindex, int len,ref Dictionary<int, Match> tmp)
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

            if (tmp.Count > 0 && tmp.Count % 2 != 0)
            {
                int i = 0;
                foreach (var item in tmp)
                {
                    ++i;
                    if (tmp.Count == 1)
                    {
                        if (item.Value.Value.Contains("</"))
                        {
                            tmp.Add(item.Key - 1, match[item.Key - 1]);
                        }
                        else
                        {
                            tmp.Add(item.Key + 1, match[item.Key + 1]);
                        }

                        break;
                    }
                    else
                    {
                        if (item.Value.Value.Contains("</") && i == 1)
                        {
                            tmp.Add(item.Key - 1, match[item.Key - 1]);
                            break;
                        }
                        else if (i == tmp.Count)
                        {
                            tmp.Add(item.Key + 1, match[item.Key + 1]);
                            break;
                        }
                    }
                }
            }
            return tmp;
        }
    }
}