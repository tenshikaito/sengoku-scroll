using Library;
using System.Collections.Generic;

namespace Core
{
    public partial class GameWording : Wording, IWording
    {
        #region references
        //public Word start => this[nameof(start)];
        //public Word start_game => this[nameof(start_game)];
        //public Word edit_game => this[nameof(edit_game)];
        //public Word none => this[nameof(none)];
        //public Word login => this[nameof(login)];
        //public Word register => this[nameof(register)];
        //public Word active => this[nameof(active)];
        //public Word edit => this[nameof(edit)];
        //public Word exit => this[nameof(exit)];
        //public Word username => this[nameof(username)];
        //public Word password => this[nameof(password)];
        //public Word email => this[nameof(email)];
        //public Word code => this[nameof(code)];
        //public Word create_general => this[nameof(create_general)];
        //public Word create_item => this[nameof(create_item)];
        //public Word edit_general => this[nameof(edit_general)];
        //public Word edit_force => this[nameof(edit_force)];
        //public Word edit_item => this[nameof(edit_item)];

        //public Word active_succeed => this[nameof(active_succeed)];
        //public Word active_failed => this[nameof(active_failed)];
        //public Word general_created => this[nameof(general_created)];
        //public Word general_create_failed => this[nameof(general_create_failed)];

        //public Word symbol_colon => this[nameof(symbol_colon)];
        //public Word symbol_none => this[nameof(symbol_none)];
        //public Word symbol_yes => this[nameof(symbol_yes)];
        //public Word symbol_no => this[nameof(symbol_no)];

        //public Word symbol_ability => this[nameof(symbol_ability)];
        //public Word symbol_half_ability => this[nameof(symbol_half_ability)];

        //public Word morpheme_team => this[nameof(morpheme_team)];

        //public Word loading_system => this[nameof(loading_system)];
        //public Word loading_game => this[nameof(loading_game)];
        //public Word initialize_ui => this[nameof(initialize_ui)];
        //public Word is_loading => this[nameof(is_loading)];

        //public Word is_building => this[nameof(is_building)];
        //public Word is_surrounded => this[nameof(is_surrounded)];
        //public Word is_broken => this[nameof(is_broken)];

        //public Word basic_data => this[nameof(basic_data)];
        //public Word advanced_data => this[nameof(advanced_data)];
        //public Word ability => this[nameof(ability)];
        //public Word skill => this[nameof(skill)];
        //public Word battle_skill => this[nameof(battle_skill)];
        //public Word transaction_skill => this[nameof(transaction_skill)];
        //public Word list_view => this[nameof(list_view)];

        //public Word financial_reporting => this[nameof(financial_reporting)];
        //public Word financial_reporting_detail => this[nameof(financial_reporting_detail)];

        //public Word type => this[nameof(type)];
        //public Word post_time => this[nameof(post_time)];
        //public Word information => this[nameof(information)];
        //public Word introduction => this[nameof(introduction)];
        //public Word comment => this[nameof(comment)];
        //public Word exiling => this[nameof(exiling)];
        //public Word resigning => this[nameof(resigning)];

        //public Word leadership => this[nameof(leadership)];
        //public Word power => this[nameof(power)];
        //public Word politics => this[nameof(politics)];
        //public Word wisdom => this[nameof(wisdom)];
        //public Word charm => this[nameof(charm)];
        //public Word spell => this[nameof(spell)];
        //public Word level => this[nameof(level)];
        //public Word culture => this[nameof(culture)];
        //public Word position => this[nameof(position)];
        //public Word kani => this[nameof(kani)];
        //public Word yakushoku => this[nameof(yakushoku)];
        //public Word home => this[nameof(home)];
        //public Word blood => this[nameof(blood)];
        //public Word religion => this[nameof(religion)];
        //public Word faith => this[nameof(faith)];
        //public Word contribution => this[nameof(contribution)];
        //public Word salary => this[nameof(salary)];
        //public Word join_time => this[nameof(join_time)];
        //public Word weapon => this[nameof(weapon)];
        //public Word mount => this[nameof(mount)];
        //public Word infantry => this[nameof(infantry)];
        //public Word ride => this[nameof(ride)];
        //public Word sailing => this[nameof(sailing)];
        //public Word archery => this[nameof(archery)];
        //public Word fight => this[nameof(fight)];
        //public Word military => this[nameof(military)];
        //public Word spy => this[nameof(spy)];
        //public Word ninjutsu => this[nameof(ninjutsu)];
        //public Word construct => this[nameof(construct)];
        //public Word reclaim => this[nameof(reclaim)];
        //public Word mineral => this[nameof(mineral)];
        //public Word arithmetic => this[nameof(arithmetic)];
        //public Word manners => this[nameof(manners)];
        //public Word eloquence => this[nameof(eloquence)];
        //public Word teaism => this[nameof(teaism)];
        //public Word healing => this[nameof(healing)];

        //public Word suzerain => this[nameof(suzerain)];
        //public Word orbit => this[nameof(orbit)];
        //public Word secular => this[nameof(secular)];
        //public Word merchant => this[nameof(merchant)];
        //public Word landlord => this[nameof(landlord)];
        //public Word bandit => this[nameof(bandit)];

        //public Word food => this[nameof(food)];
        //public Word wood => this[nameof(wood)];
        //public Word stone => this[nameof(stone)];
        //public Word gold => this[nameof(gold)];
        //public Word silver => this[nameof(silver)];
        //public Word copper => this[nameof(copper)];
        //public Word iron => this[nameof(iron)];

        //public Word product => this[nameof(product)];
        //public Word production => this[nameof(production)];
        //public Word can_repair => this[nameof(can_repair)];
        //public Word is_autumn_income => this[nameof(is_autumn_income)];

        //public Word job => this[nameof(job)];
        //public Word technology => this[nameof(technology)];
        //public Word expense => this[nameof(expense)];
        //public Word range => this[nameof(range)];
        //public Word guard => this[nameof(guard)];

        //public Word is_navy => this[nameof(is_navy)];
        //public Word is_mercenary => this[nameof(is_mercenary)];
        //public Word is_horse_rider => this[nameof(is_horse_rider)];
        //public Word is_hot_weapon => this[nameof(is_hot_weapon)];
        //public Word is_cannon => this[nameof(is_cannon)];

        //public Word spear => this[nameof(spear)];
        //public Word sword => this[nameof(sword)];
        //public Word bow => this[nameof(bow)];
        //public Word matchlock => this[nameof(matchlock)];
        //public Word armor => this[nameof(armor)];
        //public Word catapult => this[nameof(catapult)];
        //public Word ram => this[nameof(ram)];
        //public Word cannon => this[nameof(cannon)];
        //public Word horse => this[nameof(horse)];
        //public Word camel => this[nameof(camel)];
        //public Word elephant => this[nameof(elephant)];
        //public Word small_ship => this[nameof(small_ship)];
        //public Word middle_ship => this[nameof(middle_ship)];
        //public Word large_ship => this[nameof(large_ship)];

        //public Word count => this[nameof(count)];
        //public Word volume => this[nameof(volume)];

        //public Word hp => this[nameof(hp)];
        //public Word sp => this[nameof(sp)];
        //public Word ap => this[nameof(ap)];
        //public Word speed => this[nameof(speed)];
        //public Word commander => this[nameof(commander)];
        //public Word training => this[nameof(training)];
        //public Word morale => this[nameof(morale)];
        //public Word attack_point => this[nameof(attack_point)];
        //public Word damage_point => this[nameof(damage_point)];
        //public Word attack_range => this[nameof(attack_range)];
        //public Word defense_point => this[nameof(defense_point)];

        //public Word owner => this[nameof(owner)];
        //public Word price => this[nameof(price)];
        //public Word creator => this[nameof(creator)];
        //public Word status => this[nameof(status)];
        //public Word quantity => this[nameof(quantity)];
        //public Word holding => this[nameof(holding)];
        //public Word depositing => this[nameof(depositing)];
        //public Word equipping => this[nameof(equipping)];
        //public Word selling => this[nameof(selling)];

        //public Word is_self_used => this[nameof(is_self_used)];

        //public Word plain_castle => this[nameof(plain_castle)];
        //public Word hill_castle => this[nameof(hill_castle)];
        //public Word mountain_castle => this[nameof(mountain_castle)];
        //public Word town => this[nameof(town)];
        //public Word village => this[nameof(village)];
        //public Word temple => this[nameof(temple)];
        //public Word fastness => this[nameof(fastness)];

        //public Word field => this[nameof(field)];
        //public Word paddy => this[nameof(paddy)];
        //public Word fishery => this[nameof(fishery)];
        //public Word pasture => this[nameof(pasture)];
        //public Word stable => this[nameof(stable)];

        //public Word logging => this[nameof(logging)];
        //public Word stone_mine => this[nameof(stone_mine)];
        //public Word gold_mine => this[nameof(gold_mine)];
        //public Word silver_mine => this[nameof(silver_mine)];
        //public Word copper_mine => this[nameof(copper_mine)];
        //public Word iron_mine => this[nameof(iron_mine)];

        //public Word market => this[nameof(market)];
        //public Word smith => this[nameof(smith)];
        //public Word bar => this[nameof(bar)];
        //public Word inn => this[nameof(inn)];
        //public Word school => this[nameof(school)];
        //public Word hospital => this[nameof(hospital)];
        //public Word shipyard => this[nameof(shipyard)];
        //public Word port => this[nameof(port)];

        //public Word inspire => this[nameof(inspire)];

        //public Word system => this[nameof(system)];
        //public Word display_all => this[nameof(display_all)];
        //public Word hide_all => this[nameof(hide_all)];

        //public Word map_view => this[nameof(map_view)];
        //public Word map_view_stronghold => this[nameof(map_view_stronghold)];
        //public Word map_view_province => this[nameof(map_view_province)];
        //public Word map_view_force => this[nameof(map_view_force)];
        //public Word map_view_suzerain => this[nameof(map_view_suzerain)];
        //public Word map_view_orbit => this[nameof(map_view_orbit)];
        //public Word map_view_force_diplomacy => this[nameof(map_view_force_diplomacy)];
        //public Word map_view_suzerain_diplomacy => this[nameof(map_view_suzerain_diplomacy)];
        //public Word map_view_orbit_diplomacy => this[nameof(map_view_orbit_diplomacy)];

        //public Word map_tile_object_view => this[nameof(map_tile_object_view)];
        //public Word switch_terrain => this[nameof(switch_terrain)];
        //public Word switch_landform => this[nameof(switch_landform)];
        //public Word switch_stronghold => this[nameof(switch_stronghold)];
        //public Word switch_facility => this[nameof(switch_facility)];
        //public Word switch_resource => this[nameof(switch_resource)];
        //public Word switch_unit => this[nameof(switch_unit)];
        //public Word switch_territory => this[nameof(switch_territory)];

        //public Word history => this[nameof(history)];
        //public Word switch_general => this[nameof(switch_general)];
        //public Word switch_force => this[nameof(switch_force)];
        //public Word switch_item => this[nameof(switch_item)];
        //public Word switch_army => this[nameof(switch_army)];

        //public Word command => this[nameof(command)];

        //public Word move => this[nameof(move)];
        //public Word rename => this[nameof(rename)];
        //public Word sail => this[nameof(sail)];
        //public Word land => this[nameof(land)];
        //public Word dissolve => this[nameof(dissolve)];

        //public Word personal => this[nameof(personal)];
        //public Word rest => this[nameof(rest)];
        //public Word withdraw => this[nameof(withdraw)];
        //public Word deposit => this[nameof(deposit)];
        //public Word item => this[nameof(item)];
        //public Word conscript => this[nameof(conscript)];
        //public Word move_home => this[nameof(move_home)];

        //public Word force => this[nameof(force)];
        //public Word employ => this[nameof(employ)];
        //public Word ask_job => this[nameof(ask_job)];
        //public Word resign => this[nameof(resign)];
        //public Word policy => this[nameof(policy)];
        //public Word exile => this[nameof(exile)];
        //public Word abdicate => this[nameof(abdicate)];
        //public Word betray => this[nameof(betray)];
        //public Word revolutionize => this[nameof(revolutionize)];

        //public Word lord => this[nameof(lord)];
        //public Word castellan => this[nameof(castellan)];
        //public Word population => this[nameof(population)];
        //public Word tax_rate => this[nameof(tax_rate)];
        //public Word labour => this[nameof(labour)];
        //public Word growth => this[nameof(growth)];
        //public Word general => this[nameof(general)];
        //public Word capital => this[nameof(capital)];
        //public Word stored_food => this[nameof(stored_food)];

        //public Word stronghold => this[nameof(stronghold)];
        //public Word organize => this[nameof(organize)];
        //public Word train => this[nameof(train)];
        //public Word appoint => this[nameof(appoint)];
        //public Word dispatch => this[nameof(dispatch)];
        //public Word distribute => this[nameof(distribute)];
        //public Word territory => this[nameof(territory)];
        //public Word rally_point => this[nameof(rally_point)];
        //public Word move_capital => this[nameof(move_capital)];
        //public Word transfer => this[nameof(transfer)];
        //public Word release => this[nameof(release)];
        //public Word abandon => this[nameof(abandon)];

        //public Word facility => this[nameof(facility)];
        //public Word incite => this[nameof(incite)];
        //public Word buy => this[nameof(buy)];
        //public Word sell => this[nameof(sell)];
        //public Word breed => this[nameof(breed)];
        //public Word forge => this[nameof(forge)];
        //public Word gamble => this[nameof(gamble)];
        //public Word study => this[nameof(study)];
        //public Word treat => this[nameof(treat)];
        //public Word pharmaceutical => this[nameof(pharmaceutical)];
        //public Word revoke => this[nameof(revoke)];

        //public Word take => this[nameof(take)];
        //public Word put => this[nameof(put)];
        //public Word equip => this[nameof(equip)];
        //public Word unequip => this[nameof(unequip)];
        //public Word use => this[nameof(use)];
        //public Word discard => this[nameof(discard)];

        //public Word unit => this[nameof(unit)];
        //public Word relieve => this[nameof(relieve)];
        //public Word direction => this[nameof(direction)];

        //public Word army => this[nameof(army)];
        //public Word attack => this[nameof(attack)];
        //public Word shoot => this[nameof(shoot)];
        //public Word strategy => this[nameof(strategy)];
        //public Word sack => this[nameof(sack)];

        //public Word worker => this[nameof(worker)];
        //public Word develop => this[nameof(develop)];
        //public Word manufacture => this[nameof(manufacture)];
        //public Word build_stronghold => this[nameof(build_stronghold)];
        //public Word build_facility => this[nameof(build_facility)];
        //public Word road => this[nameof(road)];
        //public Word repair => this[nameof(repair)];
        //public Word extend => this[nameof(extend)];
        //public Word demolish => this[nameof(demolish)];

        //public Word transporter => this[nameof(transporter)];
        //public Word deliver => this[nameof(deliver)];
        //public Word receipt => this[nameof(receipt)];
        //public Word trade => this[nameof(trade)];

        //public Word settle => this[nameof(settle)];

        //public Word envoy => this[nameof(envoy)];
        //public Word ally => this[nameof(ally)];
        //public Word declare_war => this[nameof(declare_war)];
        //public Word harmony => this[nameof(harmony)];
        //public Word repeal => this[nameof(repeal)];
        //public Word declare_orbit => this[nameof(declare_orbit)];
        //public Word leave_orbit => this[nameof(leave_orbit)];
        //public Word cancel_orbit => this[nameof(cancel_orbit)];
        //public Word subordinate => this[nameof(subordinate)];
        //public Word surrender => this[nameof(surrender)];

        //public Word monk => this[nameof(monk)];
        //public Word research => this[nameof(research)];
        //public Word missionize => this[nameof(missionize)];
        //public Word purge => this[nameof(purge)];
        //public Word found_denomination => this[nameof(found_denomination)];
        //public Word found_religion => this[nameof(found_religion)];

        //public Word settler => this[nameof(settler)];

        //public Word accept => this[nameof(accept)];
        //public Word refuse => this[nameof(refuse)];
        //public Word accept_all => this[nameof(accept_all)];
        //public Word refuse_all => this[nameof(refuse_all)];
        //public Word all => this[nameof(all)];
        //public Word clear => this[nameof(clear)];
        //public Word clear_all => this[nameof(clear_all)];
        //public Word refresh => this[nameof(refresh)];
        //public Word export => this[nameof(export)];
        //public Word ok => this[nameof(ok)];
        //public Word cancel => this[nameof(cancel)];

        //public Word illegal_input => this[nameof(illegal_input)];
        //public Word illegal_select => this[nameof(illegal_select)];

        //public string internal_error => nameof(internal_error);
        //public string invalid_state => nameof(invalid_state);
        //public string illegal_permission => nameof(illegal_permission);
        //public string none_selection => nameof(none_selection);

        //public string select_file => nameof(select_file);
        //public Word export_succeed => nameof(export_succeed);
        //public Word export_failed => nameof(export_failed);

        #endregion

        public string id => this[nameof(id)];

        public string name => this[nameof(name)];

        public string code => this[nameof(code)];

        public string introduction => this[nameof(introduction)];

        public string create => this[nameof(create)];

        public string add => this[nameof(add)];

        public string edit => this[nameof(edit)];

        public string remove => this[nameof(remove)];

        public string delete => this[nameof(delete)];

        public string ok => this[nameof(ok)];

        public string cancel => this[nameof(cancel)];

        public string apply => this[nameof(apply)];

        public string refresh => this[nameof(refresh)];

        public string detail => this[nameof(detail)];

        public string none => this[nameof(none)];

        public string publish => this[nameof(publish)];

        public string start => this[nameof(start)];

        public string stop => this[nameof(stop)];

        public string console => this[nameof(console)];

        public string type => this[nameof(type)];

        public string width => this[nameof(width)];

        public string height => this[nameof(height)];

        public string player => this[nameof(player)];

        public string game_world => this[nameof(game_world)];

        public string main_tile_map => this[nameof(main_tile_map)];

        public string detail_tile_map => this[nameof(detail_tile_map)];

        public string map_size => this[nameof(map_size)];

        public string edit_game_world => this[nameof(edit_game_world)];

        public string stronghold => this[nameof(stronghold)];

        public string loading => this[nameof(loading)];

        public string status => this[nameof(status)];

        public string ip => this[nameof(ip)];

        public string port => this[nameof(port)];

        public string connect => this[nameof(connect)];

        public string disconnected => this[nameof(disconnected)];

        public string close => this[nameof(close)];

        public string symbol_selected => this[nameof(symbol_selected)];

        public string symbol_unselected => this[nameof(symbol_unselected)];

        public string symbol_none => this[nameof(symbol_none)];

        public string pointer => this[nameof(pointer)];

        public Terrain terrain;

        public Region region;

        public Culture culture;

        public Religion religion;

        public Road road;

        public StrongholdType stronghold_type;

        public TileMapImageInfo tile_map_image_info;
        public TerrainImage terrain_image;

        public SceneTitle scene_title;
        public SceneGame scene_game;
        public SceneEditGameWorld scene_edit_game_world;

        public GameWording(string locale, Dictionary<string, string> data) : base(locale, data)
        {
            terrain = new Terrain(this, nameof(terrain));
            region = new Region(this, nameof(region));
            culture = new Culture(this, nameof(culture));
            religion = new Religion(this, nameof(religion));
            road = new Road(this, nameof(road));
            stronghold_type = new StrongholdType(this, nameof(stronghold_type));

            tile_map_image_info = new TileMapImageInfo(this, nameof(tile_map_image_info));
            terrain_image = new TerrainImage(this, nameof(terrain_image));

            scene_title = new SceneTitle(this, nameof(scene_title));
            scene_game = new SceneGame(this, nameof(scene_game));
            scene_edit_game_world = new SceneEditGameWorld(this, nameof(scene_title));
        }

        //public string getTip(string key) => this[tips[key]];

        //public string getTip(string name, string param) => getTip($"{name}_{param}");

        //public string getTip(string type, string name, string param) => getTip($"{type}_{name}_{param}");

        //public string getDialogContent(string key) => this[dialogContents[key]];

        //public string getDialogContent(string name, string param) => getDialogContent($"{name}_{param}");

        //public string getDialogContent(string type, string name, string param) => getDialogContent($"{type}_{name}_{param}");

        //public string getCommandRecord(string key) => this[commandRecords[key]];

        //public string getCommandRecord(string name, string param) => getCommandRecord($"{name}_{param}");

        //public string getCommandRecord(string type, string name, string param) => getCommandRecord($"{type}_{name}_{param}");

        //public string getIntroduction(string key) => this[introductions[key]];

        //private Association tips = new Association("tip_{0}");
        //private Association dialogContents = new Association("dialog_content_{0}");
        //private Association commandRecords = new Association("command_message_{0}");
        //private Association introductions = new Association("introduction_{0}");
    }

    public partial class GameWording
    {
        public class Terrain : Part
        {
            public string text => this[prefix];

            public string is_surface => this[nameof(is_surface)];

            public string is_grass => this[nameof(is_grass)];

            public string is_forest => this[nameof(is_forest)];

            public string is_waste => this[nameof(is_waste)];

            public string is_desert => this[nameof(is_desert)];

            public string is_marsh => this[nameof(is_marsh)];

            public string is_rock => this[nameof(is_rock)];

            public string is_hill => this[nameof(is_hill)];

            public string is_mountain => this[nameof(is_mountain)];

            public string is_habour => this[nameof(is_habour)];

            public string is_water => this[nameof(is_water)];

            public string is_fresh_water => this[nameof(is_fresh_water)];

            public string is_deep_water => this[nameof(is_deep_water)];

            public Terrain(Wording w, string prefix) : base(w, prefix)
            {
            }
        }

        public class Region : Part
        {
            public string text => this[prefix];

            public string climate => this[nameof(climate)];

            public Region(Wording w, string prefix) : base(w, prefix)
            {
            }
        }

        public class Culture : Part
        {
            public string text => this[prefix];

            public Culture(Wording w, string prefix) : base(w, prefix)
            {
            }
        }

        public class Religion : Part
        {
            public string text => this[prefix];

            public string is_polytheism => this[nameof(is_polytheism)];

            public Religion(Wording w, string prefix) : base(w, prefix)
            {
            }
        }

        public class Road : Part
        {
            public string text => this[prefix];

            public Road(Wording w, string prefix) : base(w, prefix)
            {
            }
        }

        public class StrongholdType : Part
        {
            public string text => this[prefix];

            public StrongholdType(Wording w, string prefix) : base(w, prefix)
            {
            }
        }

        public class TileMapImageInfo : Part
        {
            public string text => this[prefix];

            public string tile_size => this[nameof(tile_size)];

            public string tile_width => this[nameof(tile_width)];

            public string tile_height => this[nameof(tile_height)];

            public string file_name => this[nameof(file_name)];

            public string frame => this[nameof(frame)];

            public string position => this[nameof(position)];

            public string interval => this[nameof(interval)];

            public string edit_success => this[nameof(edit_success)];

            public string edit_failure => this[nameof(edit_failure)];

            public TileMapImageInfo(Wording w, string prefix) : base(w, prefix)
            {
            }
        }

        public class TerrainImage : Part
        {
            public string text => this[prefix];

            public string animation_view => this[nameof(animation_view)];

            public string animation_view_spring => this[nameof(animation_view_spring)];

            public string animation_view_summer => this[nameof(animation_view_summer)];

            public string animation_view_autumn => this[nameof(animation_view_autumn)];

            public string animation_view_winter => this[nameof(animation_view_winter)];

            public string animation_view_snow => this[nameof(animation_view_snow)];

            public string animation_detail => this[nameof(animation_detail)];

            public string animation_detail_spring => this[nameof(animation_detail_spring)];

            public string animation_detail_summer => this[nameof(animation_detail_summer)];

            public string animation_detail_autumn => this[nameof(animation_detail_autumn)];

            public string animation_detail_winter => this[nameof(animation_detail_winter)];

            public string animation_detail_snow => this[nameof(animation_detail_snow)];

            public TerrainImage(Wording w, string prefix) : base(w, prefix)
            {
            }
        }
    }

    public partial class GameWording
    {
        public class SceneTitle : Part
        {
            public string start => this[nameof(start)];

            public string select_game_world => this[nameof(select_game_world)];

            public string select_game_scenario => this[nameof(select_game_scenario)];

            public string multiplayer_game => this[nameof(multiplayer_game)];

            public string select_mode => this[nameof(select_mode)];

            public string story_mode => this[nameof(story_mode)];

            public string free_mode => this[nameof(free_mode)];

            public string edit_map => this[nameof(edit_map)];

            public string select_player => this[nameof(select_player)];

            public string connecting => this[nameof(connecting)];

            public string loading => this[nameof(loading)];

            public string enter => this[nameof(enter)];

            public string join => this[nameof(join)];

            public string select_game_map => this[nameof(select_game_map)];

            public SceneTitle(Wording w, string prefix) : base(w, prefix)
            {
            }
        }
    }

    public partial class GameWording
    {
        public class SceneGame : Part
        {
            public string move => this[nameof(move)];

            public SceneGame(Wording w, string prefix) : base(w, prefix)
            {
            }
        }
    }

    public partial class GameWording
    {
        public class SceneEditGameWorld : Part
        {
            public string brush => this[nameof(brush)];

            public string rectangle => this[nameof(rectangle)];

            public string fill => this[nameof(fill)];

            public string database => this[nameof(database)];

            public string refresh => this[nameof(refresh)];

            public string save => this[nameof(save)];

            public string exit => this[nameof(exit)];

            public string property => this[nameof(property)];

            public SceneEditGameWorld(Wording w, string prefix) : base(w, prefix)
            {
            }
        }
    }
}
