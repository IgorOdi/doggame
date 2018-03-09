﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleManager : MonoBehaviour {

	public static BattleManager instance;
	public BattleAgent[] battleAgents;
	public List<HeroAgent> heroParty = new List<HeroAgent> ();
	public List<EnemyAgent> enemyParty = new List<EnemyAgent> ();

//	[HideInInspector]
	public List<int> turnQueue;
	[HideInInspector]
	public BattleAgent selectedHero;
	[HideInInspector]
	public BattleAgent selectedTarget;
	[HideInInspector]
	public bool selecting;
	[HideInInspector]
	public int agentTurn;
	[HideInInspector]
	public bool allySkill;

	[SerializeField]
	private Button[] skillButton;
	[SerializeField]
	private Button changeButton;

	private int turn;

	private int[] heroPositions = { -1, -3, -5, -7 };
	private int[] enemyPositions = { 1, 3, 5, 7};

	private const float yPos = -1f;

	private const int maxEnemies = 4;
	private const int maxAgents = 8;

	private void Awake() {

		if (instance == null) instance = this;
		turnQueue = new List<int> ();

		ConfigureButtons ();
	}

	private void ConfigureButtons() {

		skillButton [0].onClick.AddListener (delegate {

			if (selectedHero == battleAgents [agentTurn])
				StartCoroutine (Action (0));
		});

		skillButton [1].onClick.AddListener (delegate {

			if (selectedHero == battleAgents [agentTurn])
				StartCoroutine (Action (1));
		});

		skillButton [2].onClick.AddListener (delegate {

			if (selectedHero == battleAgents [agentTurn])
				StartCoroutine (Action (2));
		});

		skillButton [3].onClick.AddListener (delegate {

			if (selectedHero == battleAgents [agentTurn])
				StartCoroutine (Action (3));
		});

		changeButton.onClick.AddListener (delegate {

			if (selectedHero == battleAgents [agentTurn])
				StartCoroutine (ChangeOrder ());
		});
	}

	public void StartBattle(List<Enemy> _enemies) {

		//canMove = false;

		GameManager.instance.FadeToBattle ();

		for (int i = 0; i < _enemies.Count + maxEnemies; i++) {

			if (i < maxEnemies) {

				if (battleAgents [i].gameObject.activeSelf) {
					
					HeroAgent heroAgent = battleAgents [i].GetComponent<HeroAgent> ();
					heroParty.Add (heroAgent);
					heroAgent.position = i;
					battleAgents [i].transform.localPosition = new Vector2 (heroPositions [i], yPos);
				}

			} else {
				
				battleAgents [i].gameObject.SetActive (true);
				EnemyAgent enemyAgent = battleAgents [i].GetComponent<EnemyAgent> ();
				enemyAgent.enemyInfo = _enemies [i - maxEnemies];
				enemyAgent.position = i - maxEnemies;
				enemyAgent.DefineStats ();
				enemyParty.Add (enemyAgent);

				battleAgents [i].transform.localPosition = new Vector2 (enemyPositions [i - maxEnemies], yPos);
			}
		}

		turn = 0;
		EnqueueAgents ();
		NextTurn ();
	}

	private void EnqueueAgents() {

		List<int> speedList = new List<int> ();

		for (int i = 0; i < maxAgents; i++) {

			if (battleAgents [i].gameObject.activeSelf) {

				speedList.Add (battleAgents [i].actualInfo.spd);
			}
		}

		int activeAgents = heroParty.Count + enemyParty.Count;

		for (int i = 0; i < maxEnemies; i++) {

				HeroAgent heroAgent = battleAgents [i].GetComponent<HeroAgent> ();
				heroAgent.actionMarker.SetActive (false);
		}

		while (turnQueue.Count < activeAgents) {
			
			for (int i = 0; i < maxAgents; i++) {

				if (battleAgents [i].gameObject.activeSelf) {

					int higherValue = Mathf.Max (speedList.ToArray());

					if (battleAgents [i].actualInfo.spd == higherValue) {

						turnQueue.Add (i);
						speedList.Remove (higherValue);
					}
				}
			}
		}
	}
		
	public void NextTurn() {

		if (heroParty.Count > 0 && enemyParty.Count > 0) {

			if (turnQueue.Count > 0) {

				agentTurn = turnQueue [0];
				turnQueue.Remove (agentTurn);

				if (agentTurn < maxEnemies) {

					selectedHero = battleAgents [agentTurn];
					battleAgents [agentTurn].GetComponent<HeroAgent> ().ChangeHUD (battleAgents [agentTurn]);
					HUDManager.instance.ChangeHeroHUD (battleAgents [agentTurn].actualInfo);
				} else {
				
					HUDManager.instance.ChangeEnemyHUD (battleAgents [agentTurn].actualInfo);
				}

				HUDManager.instance.ChangeTurnHUD ();
				StartCoroutine (battleAgents [agentTurn].ChooseAction ());
			} else {

				turn++;
				EnqueueAgents ();
				NextTurn ();
			}
		} else {

			EndBattle ();
		}
	}

	public IEnumerator Action(int index) {

		HeroAgent heroAgent = selectedHero.GetComponent<HeroAgent> ();
		Skill selectedSkill = heroAgent.heroInfo.skillList [index];
		allySkill = selectedSkill.skillType == SkillType.ATTACK ? true : false;

		bool canUse = heroAgent.skillCooldown[index] <= turn ? true : false;

		if (canUse)
			selecting = true;

		while (selectedTarget == null)
			yield return null;

		if (selectedHero == battleAgents [agentTurn]) {

			heroAgent.heroInfo.skillList [index].CheckSkill (selectedHero, selectedTarget);
			heroAgent.skillCooldown [index] += selectedSkill.cooldown;
			selecting = false;
			selectedTarget = null;
			HUDManager.instance.ChangeTargetHUD (null);
		}
	}

	public void ReQueue(int index, int position, bool hero) {

		turnQueue.Remove (index);

		if (hero) {

			foreach (HeroAgent h in heroParty) {

				if (h.position > position) {

					h.position -= 1;
					StartCoroutine(RePosition(h.transform, heroPositions[h.position + 1], heroPositions[h.position]));
				}
			}
		} else {

			foreach (EnemyAgent e in enemyParty) {

				if (e.position > position) {

					e.position -= 1;
					StartCoroutine(RePosition(e.transform, enemyPositions[position + 1], enemyPositions[e.position]));
				}
			}
		}
	}

	private IEnumerator RePosition(Transform tr, float _a, float _b) {

		float t = 0;

		Vector2 a = new Vector2 (_a, yPos);
		Vector2 b = new Vector2 (_b, yPos);

		while (t < 1) {

			tr.localPosition = Vector2.Lerp (a, b, t);
			t += Time.deltaTime * 2;
			HUDManager.instance.ChangeTurnHUD ();
			yield return null;
		}
	}


	public IEnumerator ChangeOrder() {

		selecting = true;

		while ((selectedHero == null || selectedTarget == null))
			yield return null;

		int dif = selectedHero.position - selectedTarget.position;

		if (dif <= 1 && dif >= -1) {

			float t = 0;
			Vector2 a = selectedHero.transform.localPosition;
			Vector2 b = selectedTarget.transform.localPosition;
			int aPosition = selectedHero.position;
			int bPosition = selectedTarget.position;

			HeroAgent heroAgent = selectedHero.GetComponent<HeroAgent> ();
			heroAgent.ChangeHUD (selectedHero);

			while (t < 1) {

				selectedHero.transform.localPosition = Vector2.Lerp (a, b, t);
				selectedTarget.transform.localPosition = Vector2.Lerp (b, a, t);
				HUDManager.instance.ChangeTurnHUD ();
				t += Time.deltaTime * 2;
				yield return null;
			}

			selectedHero.position = bPosition;
			selectedTarget.position = aPosition;

			selectedTarget = null;
			HUDManager.instance.ChangeTargetHUD (null);
			selecting = false;
			NextTurn ();
		}
	}

	public void EndBattle() {

		if (heroParty.Count > 0) {
			
			selecting = false;
			selectedHero = null;
			selectedTarget = null;
			heroParty.Clear ();
			enemyParty.Clear ();
			turnQueue.Clear ();
			HUDManager.instance.ChangeTargetHUD (null);
			HUDManager.instance.DeactivateTurnHUD ();
			GameManager.instance.FadeOffBattle ();
		} else {

			print ("Game Over");
//			GameManager.GameOver ();
		}
	}
}