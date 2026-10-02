# 先古之民正式对话（2026-10-02）

需求：`DOC-ANCIENT-DIALOGUE-001`；状态：`IMPLEMENTED`，游戏验收 `NOT_RUN`。

用户确认27组57句正文；资源为`MaidenSuccubus/localization/zhs/ancients.json`。保持原登记、30按钮与原版拜访优先级；叙事不新增结算。全角色首次拜访公共对白可能先于天音专属初见显示；0/1/2是角色拜访次数，并非三幕。`r`标识重复对白；建筑师三组保留Both结尾攻击。

## 验收

- ANCIENT-DIALOGUE-01：87键、57正文、30按钮；无占位；说话者、组数、标签及按钮不变。
- ANCIENT-DIALOGUE-02：自然拜访、重复池与建筑师结尾攻击仍遵循既有代码。
- ANCIENT-DIALOGUE-03：用户实机查看文字、翻页、波动/停顿/强调和动作旁白，未执行。

## 已确认全文（含富文本）

### DARV

`DARV.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.0-0.char`

```text
……这堆东西下面好像还有一把剑。要不要先[b]拿出来[/b]？
```

`DARV.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.0-1.ancient`

```text
哦，那把！我找它好久啦！不过先别动，底下还有[b]一大堆[/b]呢！来来，从上面挑一样吧！
```

`DARV.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.1-0r.ancient`

```text
是你呀！上次拿的东西还好用吧？我又收了几件！仔细挑挑，有些我还没弄清[b]怎么用[/b]呢！
```

`DARV.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.2-0.ancient`

```text
会说话的书我也收过！不过，你这本看起来可[b]精神多了[/b]！怎么样，拿什么跟我换？
```

`DARV.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.2-1.char`

```text
……这个[b]不换[/b]。虽然有时候挺让人头疼的，但我还需要它。
```

`DARV.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.2-2.ancient`

```text
哈哈，好好好！那就看看别的！从那堆里挑一样吧，别让你的书把[b]整堆[/b]都挑走啦！
```

### NEOW

`NEOW.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.0-0.char`

```text
……刚才我应该已经倒下了。是你[b]救了我[/b]？
```

`NEOW.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.0-1.ancient`

```text
[sine]……醒来……我的……少女……
……你的战斗……还没有……结束……[/sine]
```

`NEOW.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.1-0r.ancient`

```text
[sine]……又回来了……
……这次……走得……更远些……[/sine]
```

`NEOW.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.2-0.ancient`

```text
[sine]……你害怕……再也……回不去……[/sine]
```

`NEOW.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.2-1.char`

```text
……当然怕啊。还有人在[b]等我[/b]。
如果你能让我继续战斗，就请帮帮我。
```

`NEOW.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.2-2.ancient`

```text
[sine]……收下……我的祝福……
……塔顶……[red]他[/red]在那里……等你……[/sine]
```

### NONUPEIPE

`NONUPEIPE.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.0-0.char`

```text
……这些都是给我的？不过，穿成这样，好像不太方便[b]战斗[/b]……
```

`NONUPEIPE.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.0-1.ancient`

```text
亲爱的，那是因为你还没穿过真正[b]合身的赐福[/b]。过来，让我看看。
```

`NONUPEIPE.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.1-0r.ancient`

```text
又把衣服弄坏了？过来吧，我可不能让接受过[b]我赐福的人[/b]这样出门。
```

`NONUPEIPE.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.2-0.ancient`

```text
这件衣装倒是很[b]衬你[/b]。是谁替你挑的？
```

`NONUPEIPE.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.2-1.char`

```text
……变身之后就这样了，我也[b]没得挑[/b]。
有时候还挺让人不好意思的……
```

`NONUPEIPE.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.2-2.ancient`

```text
抬起头来，亲爱的。穿着华丽的衣装，就应该[b]坦坦荡荡[/b]。
```

### OROBAS

`OROBAS.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.0-0.char`

```text
……你身上的颜色一直在变。我该[b]看哪里[/b]，才算是在跟你说话啊？
```

`OROBAS.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.0-1.ancient`

```text
哪里都行！！我看着你呢！新朋友，还有一本书！来看看这些，[jitter]亮闪闪！！[/jitter]
```

`OROBAS.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.1-0r.ancient`

```text
新故事？有[jitter]新故事[/jitter]吗？！我找到了新东西！你挑一个，我听故事！！
```

`OROBAS.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.2-0.ancient`

```text
[jitter]魔法！[/jitter]衣服也会变！？怎么变的？还能再变一次吗？！
```

`OROBAS.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.2-1.char`

```text
是[b]变身[/b]啦……也不是换衣服。那个，你能不能一个一个问？
```

`OROBAS.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.2-2.ancient`

```text
好！一个一个！先收下这个！！然后告诉我，[jitter]衣服藏在哪里？！[/jitter]
```

### PAEL

`PAEL.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.0-0.char`

```text
……你还醒着吗？我有件事想问……
算了，等你[b]醒过来[/b]再说吧。
```

`PAEL.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.0-1.ancient`

```text
[thinky_dots]我听着呢……只是回答需要一点时间……
你也可以坐下来……[/thinky_dots]
```

`PAEL.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.1-0r.ancient`

```text
[thinky_dots]又是你……这次也没有睡好么……？
拿走我的一部分吧……别把自己累坏了……[/thinky_dots]
```

`PAEL.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.2-0.ancient`

```text
[thinky_dots]你也要去找建筑师么……？
他最近……不太喜欢有人打扰……[/thinky_dots]
```

`PAEL.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.2-1.char`

```text
你是说[b]塔顶的那个人[/b]？
……我只是想问他几件事。应该不至于为这个生气吧？
```

`PAEL.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.2-2.ancient`

```text
[thinky_dots]唔……你可以试试……
要是他生气了……就回来吧……这里还有地方……[/thinky_dots]
```

### TANX

`TANX.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.0-0.char`

```text
……这里怎么连落脚的地方都堆着武器？
这些你真的[b]全都用得上[/b]吗？
```

`TANX.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.0-1.ancient`

```text
[b]当然！！不同的敌人，要用不同的武器！！
你也该多拿几把！！[/b]
```

`TANX.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.1-0r.ancient`

```text
[b]魔法少女！！你的武器还没坏吧！？
坏了就换一把！没坏就再拿一把！！[/b]
```

`TANX.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.2-0.ancient`

```text
[b]天音！！[jitter]与我一战！！[/jitter]让我见识你的魔法！！[/b]
```

`TANX.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.2-1.char`

```text
……我还要去塔顶呢。
现在把力气都用在这里，[b]后面怎么办啊[/b]？
```

`TANX.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.2-2.ancient`

```text
[b]好！！先去打倒塔顶的家伙！！
拿上武器！回来再与我一战！！[/b]
```

### TEZCATARA

`TEZCATARA.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.0-0.char`

```text
好香……你在烤什么？
……等等，里面刚才是不是有什么东西在[b]动[/b]？
```

`TEZCATARA.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.0-1.ancient`

```text
哎呀，眼睛真尖，亲爱的。离炉子远些吧，[b]点心[/b]还没准备好呢。
```

`TEZCATARA.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.1-0r.ancient`

```text
快进来，小可爱！让我看看，今天该替你[jitter][b]烧掉[/b][/jitter]些什么烦恼呢？
```

`TEZCATARA.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.2-0.ancient`

```text
[i][font_size=22]特兹卡塔拉高兴地咯咯笑。[/font_size][/i]
哎呀，亲爱的，袖口都烧焦了。自己的[b]火焰[/b]也会欺负你么？
```

`TEZCATARA.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.2-1.char`

```text
……刚才离得太近了。我本来想躲开，结果裙角又被勾住……
[b]你别笑啊。[/b]
```

`TEZCATARA.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.2-2.ancient`

```text
好啦，亲爱的，手伸过来，让我看看有没有烫伤。
下次遇到这么没礼貌的家伙，可得先把它的爪子[jitter][b]烧掉[/b][/jitter]。
```

### VAKUU

`VAKUU.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.0-0.char`

```text
……我还[b]没答应[/b]呢，你怎么就把契约拿出来了？
```

`VAKUU.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.0-1.ancient`

```text
不必紧张。你可以先看看自己能[sine]得到什么[/sine]。许多人读到那里，就不再急着离开了。
```

`VAKUU.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.1-0r.ancient`

```text
欢迎回来。你上次觉得[sine]太昂贵[/sine]的东西，我替你留着呢。
```

`VAKUU.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.2-0.ancient`

```text
你肯借用那本书的力量，却对我的[sine]馈赠[/sine]如此提防。真让我伤心。
```

`VAKUU.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.2-1.char`

```text
……[b]娅露丝救过我[/b]。
而且，我问你代价的时候，你已经岔开[b]两次[/b]了。
```

`VAKUU.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.2-2.ancient`

```text
呵……那我们就从你最在意的地方读起。
小字也请仔细读完。我可不愿被说成[sine]有所隐瞒[/sine]。
```

### THE_ARCHITECT

`THE_ARCHITECT.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.0-0r.ancient`

```text
[b]陌生的魔法。[/b]谁让你进来的？
```

`THE_ARCHITECT.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.0-1r.char`

```text
……这个问题我也想问。
如果你知道[b]出去的方法[/b]，就请告诉我。
```

`THE_ARCHITECT.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.0-2r.ancient`

```text
你一路闯到这里，只是为了问路？[b]荒唐。[/b]
```

`THE_ARCHITECT.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.1-0r.ancient`

```text
涅奥又把你拼好了。她倒很喜欢这个[b]新玩具[/b]。
```

`THE_ARCHITECT.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.1-1r.char`

```text
……[b]玩具[/b]？
[jitter]你们把人当成什么了？[/jitter]
```

`THE_ARCHITECT.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.1-2r.ancient`

```text
你应该去问[b]她[/b]。
```

`THE_ARCHITECT.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.2-0r.ancient`

```text
那本书也教不会你[b]适可而止[/b]么？
```

`THE_ARCHITECT.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.2-1r.char`

```text
……这是[b]我自己[/b]决定的。
```

`THE_ARCHITECT.talk.MAIDEN_SUCCUBUS_CHARACTER_MAIDEN_SUCCUBUS_CHARACTER.2-2r.ancient`

```text
那就由你[b]自己承担[/b]。
```


## 本轮验证结果

Debug无部署构建：`dotnet build MaidenSuccubus.csproj -c Debug --no-restore -p:DeployMod=false -p:ValidateMod=false`，0警告、0错误。游戏v0.111.0内置引擎无界面挂载候选PCK，648个资源SHA-256及57正文/30按钮验证通过。与安装前PCK比较，只有`MaidenSuccubus/localization/zhs/ancients.json`改变，647条其他资源逐字节相同。尚未运行自然对话或验证实际富文本视觉；不标VERIFIED。资源部署在范围提交后进行。
